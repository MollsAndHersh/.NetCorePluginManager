﻿/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *  .Net Core Plugin Manager is distributed under the GNU General Public License version 3 and  
 *  is also available under alternative licenses negotiated directly with Simon Carter.  
 *  If you obtained Service Manager under the GPL, then the GPL applies to all loadable 
 *  Service Manager modules used on your system as well. The GPL (version 3) is 
 *  available at https://opensource.org/licenses/GPL-3.0
 *
 *  This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *  See the GNU General Public License for more details.
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2018 - 2020 Simon Carter.  All Rights Reserved.
 *
 *  Product:  Sitemap.Plugin
 *  
 *  File: SitemapMiddleware.cs
 *
 *  Purpose:  
 *
 *  Date        Name                Reason
 *  26/07/2019  Simon Carter        Initially Created
 *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using PluginManager.Abstractions;

using Shared.Classes;

using SharedPluginFeatures;

#pragma warning disable CS1591

namespace Sitemap.Plugin
{
    /// <summary>
    /// Sitemap middleware class, this module extends BaseMiddlware and is injected into the request pipeline.
    /// </summary>
    public sealed class SitemapMiddleware : BaseMiddleware, INotificationListener
    {
        #region Private Members

        private const string IndexStart = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
            "<sitemapindex xmlns=\"https://www.sitemaps.org/schemas/sitemap/0.9\">";
        private const string IndexEnd = "\r\n</sitemapindex>";
        private const string IndexItem = "\r\n\t<sitemap>\r\n\t\t<loc>{0}sitemap{1}.xml</loc>\r\n\t\t<lastmod>{2}</lastmod>\r\n\t</sitemap>";
        private const string SiteItemStart = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<urlset xmlns=\"https://www.sitemaps.org/schemas/sitemap/0.9\">";
        private const string SiteItemEnd = "\r\n</urlset>\r\n";
        private const string SiteItem = "\r\n\t<url>\r\n\t\t<loc>{0}</loc>\r\n\t\t<changefreq>{1}</changefreq>";
        private const string SiteItemLastModified = "\r\n\t\t<lastmod>{0}</lastmod>";
        private const string SiteItemPriority = "\r\n\t\t<priority>{0}</priority>";
        private const string SiteItemEndItem = "\r\n\t</url>";
        private const ushort MaxSitemapItemsPerFile = 25000;

        private readonly RequestDelegate _next;
        private readonly IMemoryCache _memoryCache;
        private readonly IPluginClassesService _pluginClassesService;
        private readonly List<string> _sitemaps;
        private readonly object _lockObject = new object();
        private readonly string mainSitemap = $"{Constants.ForwardSlashChar}{Constants.BaseSitemap}";
        internal static Timings _timings = new Timings();

        #endregion Private Members

        #region Constructors

        public SitemapMiddleware(RequestDelegate next, IPluginClassesService pluginClassesService,
            IMemoryCache memoryCache, INotificationService notificationService)
        {
            if (notificationService == null)
                throw new ArgumentNullException(nameof(notificationService));

            _next = next;
            _pluginClassesService = pluginClassesService ?? throw new ArgumentNullException(nameof(pluginClassesService));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

            _sitemaps = new List<string>();

            notificationService.RegisterListener(this);
        }

        #endregion Constructors

        #region Public Methods

        public async Task Invoke(HttpContext context)
        {
            using (StopWatchTimer stopwatchTimer = StopWatchTimer.Initialise(_timings))
            {
                string route = RouteLowered(context);

                if (route.StartsWith("/sitemap"))
                {
                    Dictionary<string, string> sitemaps = GetSitemaps(context);

                    if (sitemaps.ContainsKey(route))
                    {
                        await context.Response.WriteAsync(sitemaps[route]);
                        context.Response.StatusCode = 200;
                        return;
                    }
                }
            }

            await _next(context);
        }

        #region INotificationListener Methods

        public bool EventRaised(in string eventId, in object param1, in object param2, ref object result)
        {
            if (eventId == Constants.NotificationSitemapNames)
            {
                result = GetSitemaps((HttpContext)param1).Keys.ToList();

                return true;
            }

            return false;
        }

        public void EventRaised(in string eventId, in object param1, in object param2)
        {

        }

        public List<string> GetEvents()
        {
            return new List<string>() { Constants.NotificationSitemapNames };
        }

        #endregion INotificationListener Methods

        #endregion Public Methods

        #region Private Methods

        private Dictionary<string, string> GetSitemaps(HttpContext context)
        {
            CacheItem sitemapCache = null;

            using (TimedLock timedLock = TimedLock.Lock(_lockObject))
            {
                sitemapCache = _memoryCache.GetCache().Get(Constants.CacheSitemaps);

                if (sitemapCache == null)
                {
                    sitemapCache = new CacheItem(Constants.CacheSitemaps, GetSitemaps(GetHost(context)));
                    _memoryCache.GetCache().Add(Constants.CacheSitemaps, sitemapCache);
                }
            }

            return (Dictionary<string, string>)sitemapCache.Value;
        }

        private Dictionary<string, string> GetSitemaps(string hostDomain)
        {
            Dictionary<string, string> Result = new Dictionary<string, string>();
            Result.Add(mainSitemap, null);
            StringBuilder rootFile = new StringBuilder(IndexStart);

            List<ISitemapProvider> providers = _pluginClassesService.GetPluginClasses<ISitemapProvider>();
            ushort indexCount = 0;
            StringBuilder sitemapItem = new StringBuilder();

            foreach (ISitemapProvider provider in providers)
            {
                ushort itemCount = 0;

                foreach (SitemapItem item in provider.Items())
                {
                    if (itemCount == 0 || itemCount == MaxSitemapItemsPerFile)
                    {
                        indexCount++;
                        rootFile.Append(String.Format(IndexItem, hostDomain,
                            indexCount, DateTime.Now.ToString(Constants.W3CDateFormat)));
                        sitemapItem.Clear();
                        sitemapItem.Append(SiteItemStart);
                    }

                    sitemapItem.Append(String.Format(SiteItem,
                        item.Location.IsAbsoluteUri ? item.Location.ToString() : hostDomain + item.Location.ToString(),
                        item.ChangeFrequency.ToString().ToLower()));

                    if (item.LastModified.HasValue)
                    {
                        sitemapItem.Append(String.Format(SiteItemLastModified, item.LastModified.Value.ToString(Constants.W3CDateFormat)));
                    }

                    if (item.Priority.HasValue)
                    {
                        sitemapItem.Append(String.Format(SiteItemPriority, Math.Round(item.Priority.Value, 1)));
                    }

                    sitemapItem.Append("\r\n\t</url>");

                    itemCount++;

                    if (itemCount == MaxSitemapItemsPerFile)
                    {
                        sitemapItem.Append(SiteItemEnd);
                        Result[$"/sitemap{indexCount}.xml"] = sitemapItem.ToString();
                        itemCount = 0;
                    }
                }

                if (!Result.ContainsKey($"/sitemap{indexCount}.xml"))
                {
                    sitemapItem.Append(SiteItemEnd);
                    Result[$"/sitemap{indexCount}.xml"] = sitemapItem.ToString();
                }
            }

            rootFile.Append(IndexEnd);
            Result[mainSitemap] = rootFile.ToString();

            return Result;
        }

        #endregion Private Methods
    }
}

#pragma warning restore CS1591