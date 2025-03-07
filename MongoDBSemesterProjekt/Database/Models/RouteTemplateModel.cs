using MongoDB.Bson;

namespace MongoDBSemesterProjekt.Database.Models
{
	public class RouteTemplateModel : EntityBase
	{
		public const string CollectionName = "route-templates";

		public required string UrlTemplate { get; set; }
		public ObjectId? CollectionId { get; set; }
		public string? TemplateId { get; set; }
		public FieldMatchModel[]? Fields { get; set; }
	}
}
