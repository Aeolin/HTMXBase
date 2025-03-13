using AutoMapper;
using MongoDBSemesterProjekt.Api.Models;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Api.Requests
{
	[AutoMap(typeof(RouteTemplateModel))]
	public class ApiRouteTemplateRequest
	{
		public string? UrlTemplate { get; set; }
		public string? CollectionSlug { get; set; }
		public string? TemplateSlug { get; set; }
		public bool Paginate { get; set; }
		public ApiFieldMatchModel[]? Fields { get; set; }
	}
}
