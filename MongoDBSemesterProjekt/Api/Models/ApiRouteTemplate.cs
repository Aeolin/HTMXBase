using AutoMapper;
using AutoMapper.Configuration.Annotations;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDB.Bson;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Api.Models
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
		public string? StaticTemplate { get; set; }
		public string? RedirectUrl { get; set; }
		public string? VirtualPathTemplate { get; set; }
		public bool Paginate { get; set; }
		public int PaginationLimit { get; set; }
		public bool PaginateAscending { get; set; }

		[UpdateProperty(CollectionHandling = CollectionHandling.Set)]
		public string[]? PaginationColumns { get; set; }

		[UpdatePropertyIgnore]
		public ApiFieldMatchModel[]? Fields { get; set; }

	}
}
