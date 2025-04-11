using AutoMapper;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using HTMXBase.Api.Models;
using HTMXBase.Api.Requests;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HTMXBase.Database.Models
{
	[AutoMap(typeof(ApiRouteTemplate))]
	[AutoMap(typeof(ApiRouteTemplateCreateRequest))]
	public class RouteTemplateModel : EntityBase
	{
		public const string CollectionName = "route-templates";

		[Index(IsUnique = true)]
		public required string UrlTemplate { get; set; }

		public string? VirtualPathTemplate { get; set; }
		public string? RedirectUrl { get; set; }
		public string? CollectionSlug { get; set; }
		public string? TemplateSlug { get; set; }
		public string? StaticTemplate { get; set; }
		public bool Paginate { get; set; }

		[Range(1, 250)]
		public int PaginationLimit { get; set; } = 20;
		
		public string[]? PaginationColumns { get; set; }

		public bool PaginateAscending { get; set; } = true;

		public FieldMatchModel[]? Fields { get; set; }

		[BsonIgnore]
		[JsonIgnore]
		public bool IsStaticContentAlias => string.IsNullOrEmpty(VirtualPathTemplate) == false;

		[BsonIgnore]
		[JsonIgnore]
		public bool IsRedirect => string.IsNullOrEmpty(RedirectUrl) == false;
	}
}
