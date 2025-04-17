using MongoDB.Bson;
using HTMXBase.Database.Models;
using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;

namespace HTMXBase.Services.TemplateRouter
{
	public struct RouteMatch
	{
		public string? CollectionSlug { get; init; }
		public string? TemplateSlug { get; init; }
		public IReadOnlyDictionary<string, object?> QueryValues { get; init; }
		public RouteTemplateModel RouteTemplateModel { get; init; }
		public string? ConstructedUrl { get; init; }
		public string? ConstructedBaseTemplatePath { get; init; }

		[MemberNotNullWhen(true, nameof(ConstructedBaseTemplatePath))]
		public bool HasBaseTemplate => string.IsNullOrEmpty(ConstructedBaseTemplatePath) == false;

		[MemberNotNullWhen(true, nameof(ConstructedUrl))]
		public bool IsRedirect { get; init; }

		[MemberNotNullWhen(true, nameof(ConstructedUrl))]
		public bool IsStaticContentAlias { get; init; }

		public RouteMatch(string constructedUrl, bool isRedirect, bool isStaticContentAlias, string? baseTemplatePath, RouteTemplateModel routeTemplateModel)
		{
			this.IsStaticContentAlias = isStaticContentAlias;
			this.IsRedirect = isRedirect;
			this.ConstructedUrl = constructedUrl;
			this.RouteTemplateModel = routeTemplateModel;
			this.QueryValues = new Dictionary<string, object?>();
			this.ConstructedBaseTemplatePath = baseTemplatePath;
		}

		public RouteMatch(string collectionSlug, string? templateSlug, Dictionary<string, object?> queryValues, string? baseTemplatePath, RouteTemplateModel routeTemplateModel)
		{
			this.CollectionSlug=collectionSlug;
			this.TemplateSlug=templateSlug;
			this.QueryValues=queryValues;
			this.RouteTemplateModel=routeTemplateModel;
			this.ConstructedBaseTemplatePath=baseTemplatePath;
		}
	}
}
