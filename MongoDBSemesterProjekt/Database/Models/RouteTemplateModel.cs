using AutoMapper;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDBSemesterProjekt.Api.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace MongoDBSemesterProjekt.Database.Models
{
	[AutoMap(typeof(ApiRouteTemplate))]
	public class RouteTemplateModel : EntityBase
	{
		public const string CollectionName = "route-templates";

		[Index(IsUnique = true)]
		public required string UrlTemplate { get; set; }

		public string? RedirectUrl { get; set; }
		public string? CollectionSlug { get; set; }
		public string? TemplateSlug { get; set; }
		public string? StaticTemplate { get; set; }
		public bool Paginate { get; set; }
		public FieldMatchModel[]? Fields { get; set; }

		[BsonIgnore]
		[JsonIgnore]
		public bool IsRedirect => string.IsNullOrEmpty(RedirectUrl) == false;	
	}
}
