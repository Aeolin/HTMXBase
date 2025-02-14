using MongoDB.Bson;

namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public struct RouteMatch
	{
		public ObjectId CollectionId { get; set; }
		public string? TemplateSlug { get; set; }
		public Dictionary<string, object> QueryValues { get; set; }
	}
}
