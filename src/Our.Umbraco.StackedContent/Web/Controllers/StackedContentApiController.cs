﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Our.Umbraco.InnerContent.Helpers;
using Our.Umbraco.StackedContent.Models;
using Our.Umbraco.StackedContent.Web.Helpers;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.Routing;
using Umbraco.Web.WebApi;

namespace Our.Umbraco.StackedContent.Web.Controllers
{
    [PluginController("StackedContent")]
    public class StackedContentApiController : UmbracoAuthorizedApiController
    {
        [HttpPost]
        public HttpResponseMessage GetPreviewMarkup([FromBody] JObject item, int pageId)
        {
            var page = default(IPublishedContent);

            // If the page is new, then the ID will be zero
            if (pageId > 0)
            {
                // TODO: Review. Previewing multiple blocks on the same page will make subsequent calls to the ContentService. Is it cacheable? [LK:2018-12-12]
                // Get page container node, otherwise it's unpublished then fake PublishedContent (by IContent object)
                page = UmbracoContext.ContentCache.GetById(pageId) ?? new UnpublishedContent(pageId, Services);

                // Ensure PublishedContentRequest exists, just in case there are any RTE Macros to render
                if (UmbracoContext.PublishedContentRequest == null)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var pcr = new PublishedContentRequest(new Uri(page.UrlAbsolute()), UmbracoContext.RoutingContext);
#pragma warning restore CS0618 // Type or member is obsolete

                    UmbracoContext.PublishedContentRequest = pcr;
                    UmbracoContext.PublishedContentRequest.PublishedContent = page;
                    UmbracoContext.PublishedContentRequest.Prepare();
                }
            }

            // Convert item
            var content = InnerContentHelper.ConvertInnerContentToPublishedContent(item, page);

            // Construct preview model
            var model = new PreviewModel { Page = page, Item = content };

            // Render view
            var markup = ViewHelper.RenderPartial(content.DocumentTypeAlias, model);

            // Return response
            var response = new HttpResponseMessage
            {
                Content = new StringContent(markup ?? string.Empty)
            };

            response.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Text.Html);

            return response;
        }
    }
}