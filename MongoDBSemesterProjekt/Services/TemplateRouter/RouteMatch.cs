using MongoDB.Bson;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public struct RouteMatch
	{
		public string CollectionSlug { get; init; }
		public string? TemplateSlug { get; init; }
		public Dictionary<string, object?> QueryValues { get; init; }
		public RouteTemplateModel RouteTemplateModel { get; init; }

		public RouteMatch(string collectionSlug, string? templateSlug, Dictionary<string, object?> queryValues, RouteTemplateModel routeTemplateModel)
		{
			CollectionSlug=collectionSlug;
			TemplateSlug=templateSlug;
			QueryValues=queryValues;
			RouteTemplateModel=routeTemplateModel;
		}
	}
}
