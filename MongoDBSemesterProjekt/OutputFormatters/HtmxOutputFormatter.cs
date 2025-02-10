using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using MongoDBSemesterProjekt.Controllers;
using MongoDBSemesterProjekt.Services.TemplateStore;
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

			var descriptor = context?.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
			if (descriptor == null)
				return false;

			if(descriptor.ControllerTypeInfo != typeof(CollectionController) || descriptor.MethodInfo.Name != nameof(CollectionController.QueryAsync))
				return false;

			if (TryGetTemplateId(context?.HttpContext, out var templateId) == false)
				return false;

			if(context!.HttpContext.Request.RouteValues.TryGetValue("collectionId", out var collectionId) == false || collectionId is not string collectionIdStr)
				return false;

			var store = context!.HttpContext.RequestServices.GetRequiredService<IHtmxTemplateStore>();
			if (store.HasTemplate(collectionIdStr, templateId) == false)
				return false;

			return base.CanWriteResult(context);
		}

		public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
		{

			var store = context.HttpContext.RequestServices.GetRequiredService<IHtmxTemplateStore>();
			if(TryGetTemplateId(context.HttpContext, out var templateId) == false)
				throw new InvalidOperationException("TemplateId not found");

			if(context.HttpContext.Request.RouteValues.TryGetValue("collectionId", out var collectionId) == false || collectionId is not string collectionIdStr)
				throw new InvalidOperationException("CollectionId not found");

			var template = await store.GetTemplateAsync(collectionIdStr, templateId);
			var model = context.Object;
			var html = template(model);
			using var output = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding);
			output.Write(html);
		}
	}
}
