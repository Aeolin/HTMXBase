using MongoDB.Bson;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using AutoMapper;
using HTMXBase.Database.Models;
using AutoMapper.Configuration.Annotations;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace HTMXBase.Api.Models
{
	[AutoMap(typeof(UserModel))]
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToUserUpdate")]
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToAdminUpdate")]
	public class ApiUser
	{
		[UpdatePropertyIgnore]
		public ObjectId Id { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string? Username { get; set; }

		[JsonIgnore]
		[UpdateProperty(ApplyToAllMethods = true, TargetPropertyName = "NormalizedUsername")]
		public string? NormalizedUsername => Username?.ToLower();

		[UpdateProperty(ApplyToAllMethods = true)]
		public string? Email { get; set; }

		[UpdateProperty(MethodName = "ToAdminUpdate")]
		public bool IsLockoutEnabled { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string? FirstName { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string? LastName { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string? AvatarUrl { get; set; }

		[Ignore]
		[UpdatePropertyIgnore]
		public ApiGroup[]? Groups { get; set; }

		[UpdatePropertyIgnore]
		public string[]? Permissions { get; set; }
	}
}
