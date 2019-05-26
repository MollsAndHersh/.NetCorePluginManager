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
 *  Copyright (c) 2012 - 2019 Simon Carter.  All Rights Reserved.
 *
 *  Product:  SharedPluginFeatues
 *  
 *  File: IDocumentationService.cs
 *
 *  Purpose:  Interface providing documentation services to build and customise documentation
 *
 *  Date        Name                Reason
 *  19/05/2019  Simon Carter        Initially Created
 *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
using System;
using System.Collections.Generic;
using System.Text;

using Shared.Docs;

namespace SharedPluginFeatures
{
    /// <summary>
    /// Provides services that enable DocumentationPlugin to build documentation based on xml documentation files.
    /// </summary>
    public interface IDocumentationService
    {
        /// <summary>
        /// Retrieves a list of all system generated documentation files.
        /// </summary>
        /// <returns>:ist&lt;string&gt;</returns>
        List<string> GetDocumentationFiles();

        /// <summary>
        /// Returns a list of all available documents which have been dynamically loaded from xml documentation files.
        /// </summary>
        /// <returns>List&lt;Document&gt;</returns>
        List<Document> GetDocuments();

        /// <summary>
        /// Returns custom text for an indiviudual property within the Documentation.
        /// </summary>
        /// <param name="name">Name of custom data to be returned.</param>
        /// <param name="defaultValue">Default value that will be used.</param>
        /// <returns>string</returns>
        string GetCustomData(in string name, in string defaultValue);

        /// <summary>
        /// Returns the custom sort order for a document
        /// </summary>
        /// <param name="name">Document name</param>
        /// <param name="defaultValue">Default Value if no value has been stored.</param>
        /// <returns></returns>
        int GetCustomSortOrder(in string name, in int defaultValue);

        /// <summary>
        /// Processes a document, providing an opportunity to obtain custom property values
        /// </summary>
        /// <param name="document">Document to be processed.</param>
        void ProcessDocument(in Document document);

        /// <summary>
        /// Processes a document field, providing an opportunity to obtain custom property values
        /// </summary>
        /// <param name="field">DocumentField to be processed.</param>
        void ProcessDocumentField(in DocumentField field);

        /// <summary>
        /// Processes a document method, providing an opportunity to obtain custom property values
        /// </summary>
        /// <param name="method">DocumentField to be processed.</param>
        void ProcessDocumentMethod(in DocumentMethod method);

        /// <summary>
        /// Processes a document parameter, providing an opportunity to obtain custom property values
        /// </summary>
        /// <param name="parameter">DocumentMethodParameter to be processed.</param>
        void ProcessDocumentMethodParameter(in DocumentMethodParameter parameter);

        /// <summary>
        /// Processes a document property, providing an opportunity to obtain custom property values
        /// </summary>
        /// <param name="property">DocumentProperty to be processed.</param>
        void ProcessDocumentProperty(in DocumentProperty property);
    }
}