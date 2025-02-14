using MongoDB.Bson;

namespace MongoDBSemesterProjekt.Models
{
	public class RouteTemplateModel
	{
		public required string UrlTemplate { get; set; }
		public ObjectId? CollectionId { get; set; }
		public string? TemplateId { get; set; }
		public FieldMatchModel[]? Fields { get; set; }
	}
}
