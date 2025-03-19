using AutoMapper;
using MongoDBSemesterProjekt.Api.Models;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Api.Requests
{
	public class ApiRouteTemplateCreateRequest
	{
		public string? UrlTemplate { get; set; }
		public string? CollectionSlug { get; set; }
		public string? TemplateSlug { get; set; }
		public string? StaticTemplate { get; set; }
		public string? RedirectUrl { get; set; }
		public string? VirtualPathTemplate { get; set; }
		public bool Paginate { get; set; }
		public int? PaginationLimit;
		public string[]? PaginationColumns { get; set; }
		public bool? PaginateAscending;

		public ApiFieldMatchModel[]? Fields { get; set; }
	}
}
