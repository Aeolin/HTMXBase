using AutoMapper;
using MongoDB.Bson;
using HTMXBase.Api.Models;
using AwosFramework.Generators.MongoDBUpdateGenerator;

namespace HTMXBase.Database.Models
{
	[AutoMap(typeof(ApiFieldMatchModel))]
	[MongoDBUpdate(typeof(RouteTemplateModel), MethodName = "ToAddField")]
	[UpdateProperty(MethodName = "ToAddField", TargetPropertyName = nameof(RouteTemplateModel.Fields), CollectionHandling = CollectionHandling.PushAll)]
	public class FieldMatchModel
	{
		public required string ParameterName { get; set; }
		public required string DocumentFieldName { get; set; }
		public MatchKind MatchKind { get; set; }
		public BsonType BsonType { get; set; }
		public bool IsOptional { get; set; }
		public bool IsNullable { get; set; }
		public bool UrlEncode { get; set; }
	}
}
