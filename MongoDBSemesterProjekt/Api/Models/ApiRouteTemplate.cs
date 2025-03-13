using AutoMapper;
using AutoMapper.Configuration.Annotations;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDB.Bson;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Api.Models
{
	[MongoDBUpdate(typeof(RouteTemplateModel))]
	public class ApiRouteTemplate 
	{
		[UpdatePropertyIgnore]
		public ObjectId? Id { get; set; }
		public string? UrlTemplate { get; set; }
		public string? CollectionSlug { get; set; }
		public string? TemplateSlug { get; set; }
		public bool Paginate { get; set; }

		[UpdatePropertyIgnore]
		public ApiFieldMatchModel[]? Fields { get; set; }

	}
}
