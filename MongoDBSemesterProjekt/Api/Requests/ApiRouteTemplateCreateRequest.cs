using AutoMapper;
using MongoDBSemesterProjekt.Api.Models;
using MongoDBSemesterProjekt.Database.Models;
using System.ComponentModel.DataAnnotations;

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
		
		[Range(1, 250)]
		public int PaginationLimit { get; set; } = 20;
		public string[]? PaginationColumns { get; set; }
		public bool? PaginateAscending { get; set; }

		public ApiFieldMatchModel[]? Fields { get; set; }
	}
}
