using AutoMapper;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDB.Bson;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Api.Models
{
	[AutoMap(typeof(FieldMatchModel))]
	[MongoDBUpdate(typeof(RouteTemplateModel), NestedProperty = "Fields[$]", MethodName = "ToAdd")]
	public class ApiFieldMatchModel
	{
		public string ParameterName { get; set; }
		
		[UpdateProperty(MethodName = "ToAdd")]
		public string DocumentFieldName { get; set; }
		
		[UpdateProperty(MethodName = "ToAdd")]
		public MatchKind MatchKind { get; set; }
		
		[UpdateProperty(MethodName = "ToAdd")]
		public BsonType BsonType { get; set; }
		
		[UpdateProperty(MethodName = "ToAdd")]
		public bool IsOptional { get; set; }
		
		[UpdateProperty(MethodName = "ToAdd")]
		public bool IsNullable { get; set; }
	}
}
