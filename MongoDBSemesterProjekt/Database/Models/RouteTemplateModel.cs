using AutoMapper;
using MongoDB.Bson;
using MongoDBSemesterProjekt.Api.Models;

namespace MongoDBSemesterProjekt.Database.Models
{
	[AutoMap(typeof(ApiRouteTemplate))]
	public class RouteTemplateModel : EntityBase
	{
		public const string CollectionName = "route-templates";

		[Index(IsUnique = true)]
		public required string UrlTemplate { get; set; }
		public string? CollectionSlug { get; set; }
		public string? TemplateSlug { get; set; }
		public bool Paginate { get; set; }
		public FieldMatchModel[]? Fields { get; set; }
	}
}
