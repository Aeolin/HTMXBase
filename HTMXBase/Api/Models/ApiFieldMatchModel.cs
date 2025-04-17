using AutoMapper;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDB.Bson;
using HTMXBase.Database.Models;

namespace HTMXBase.Api.Models
{
	[AutoMap(typeof(FieldMatchModel))]
	[MongoDBUpdate(typeof(RouteTemplateModel), NestedProperty = "Fields[$]", MethodName = "ToUpdate")]
	public class ApiFieldMatchModel
	{
		public string ParameterName { get; set; }
		
		[UpdateProperty(MethodName = "ToUpdate")]
		public string DocumentFieldName { get; set; }
		
		[UpdateProperty(MethodName = "ToUpdate")]
		public MatchKind MatchKind { get; set; }
		
		[UpdateProperty(MethodName = "ToUpdate")]
		public UrlQueryBsonType BsonType { get; set; }
		
		[UpdateProperty(MethodName = "ToUpdate")]
		public bool IsOptional { get; set; }
		
		[UpdateProperty(MethodName = "ToUpdate")]
		public bool IsNullable { get; set; }

		[UpdateProperty(MethodName = "ToUpdate")]
		public bool UrlEncode { get; set; }

		[UpdateProperty(MethodName = "ToUpdate")]
		public string? Value { get; set; }
	}
}
