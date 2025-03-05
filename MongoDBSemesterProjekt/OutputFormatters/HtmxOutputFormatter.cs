using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Controllers;
using MongoDBSemesterProjekt.Services.TemplateStore;
using MongoDBSemesterProjekt.Utils;
using System.Reflection.Metadata;
using System.Text;

namespace MongoDBSemesterProjekt.OutputFormatters
{
	public class HtmxOutputFormatter : TextOutputFormatter
	{

		public HtmxOutputFormatter()
		{
			SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/html"));
			SupportedEncodings.Add(Encoding.UTF8);
			SupportedEncodings.Add(Encoding.Unicode);
		}


		private bool TryGetTemplateId(HttpContext? context, out string templateId)
		{
			if (context == null)
			{
				templateId = null;
				return false;
			}

			var req = context.Request;
			if (req.Query.TryGetValue("templateId", out var templateIds) == false)
			{
				if (req.Headers.TryGetValue("Hx-Template", out templateIds) == false)
					templateId = default;
			}

			templateId = templateIds.FirstOrDefault();
			return templateIds.Any();
		}

		public override bool CanWriteResult(OutputFormatterCanWriteContext context)
		{
			if (context == null || context.Object == null)
				return false;

			var group = context?.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName;
			if (group != Constants.HTMX_ENDPOINT)
				return false;

			if (TryGetTemplateId(context?.HttpContext, out var templateId) == false)
				return false;

			var mongoCollection = context.HttpContext.GetEndpoint().Metadata.GetMetadata<EndpointMongoCollectionAttribute>();
			string collectionSlug = mongoCollection.CollectionSlug;
			if (collectionSlug == null)
			{
				if (context.HttpContext.Request.RouteValues.TryGetValue("collectionSlug", out var collectionId) == false || collectionId is not string collectionIdStr)
					return false;

				collectionSlug = collectionIdStr;
			}

			var store = context!.HttpContext.RequestServices.GetRequiredService<IHtmxTemplateStore>();
			if (store.HasTemplate(collectionSlug, templateId) == false)
				return false;

			return base.CanWriteResult(context);
		}

		public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
		{
				var store = context.HttpContext.RequestServices.GetRequiredService<IHtmxTemplateStore>();
			if (TryGetTemplateId(context.HttpContext, out var templateId) == false)
				throw new InvalidOperationException("TemplateId not found");

			var mongoCollection = context.HttpContext.GetEndpoint().Metadata.GetMetadata<EndpointMongoCollectionAttribute>();
			string collectionSlug = mongoCollection.CollectionSlug;
			if (collectionSlug == null)
			{
				if (context.HttpContext.Request.RouteValues.TryGetValue("collectionSlug", out var collectionId) == false || collectionId is not string collectionIdStr)
					throw new InvalidOperationException("CollectionSlug not found");

				collectionSlug = collectionIdStr;
			}
	
			var template = await store.GetTemplateAsync(collectionSlug, templateId);
			var model = context.Object;
			var html = template(model);
			await context.HttpContext.Response.BodyWriter.WriteAsync(selectedEncoding.GetBytes(html));
		}
	}
}
