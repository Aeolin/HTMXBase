using MongoDB.Bson;

namespace MongoDBSemesterProjekt.Database.Models
{
	public class RouteTemplateModel : EntityBase
	{
		public const string CollectionName = "route-templates";

		public required string UrlTemplate { get; set; }
		public string? CollectionSlug { get; set; }
		public string? TemplateSlug { get; set; }
		public bool Paginate { get; set; }
		public FieldMatchModel[]? Fields { get; set; }
	}
}
