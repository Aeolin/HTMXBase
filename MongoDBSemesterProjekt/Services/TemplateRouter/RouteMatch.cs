using MongoDB.Bson;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public struct RouteMatch
	{
		public ObjectId CollectionId { get; init; }
		public string? TemplateSlug { get; init; }
		public Dictionary<string, object> QueryValues { get; init; }
		public RouteTemplateModel RouteTemplateModel { get; init; }
	}
}
