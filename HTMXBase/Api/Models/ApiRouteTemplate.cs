using AutoMapper;
using AutoMapper.Configuration.Annotations;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDB.Bson;
using HTMXBase.Database.Models;
using System.ComponentModel.DataAnnotations;

namespace HTMXBase.Api.Models
{
	[MongoDBUpdate(typeof(RouteTemplateModel))]
	[AutoMap(typeof(RouteTemplateModel))]
	public class ApiRouteTemplate 
	{
		[UpdatePropertyIgnore]
		public ObjectId? Id { get; set; }
		public string? UrlTemplate { get; set; }
		public string? CollectionSlug { get; set; }
		public string? TemplateSlug { get; set; }

		public string? BaseTemplatePathTemplate { get; set; }
		public string? RedirectUrlTemplate { get; set; }
		public string? VirtualPathTemplate { get; set; }
		public bool Paginate { get; set; }

		[Range(1, 250)]
		public int PaginationLimit { get; set; }

		public bool PaginateAscending { get; set; } = true;

		[UpdateProperty(CollectionHandling = CollectionHandling.Set)]
		public string[]? PaginationColumns { get; set; }

		[UpdatePropertyIgnore]
		public ApiFieldMatchModel[]? Fields { get; set; }

	}
}
