using MongoDB.Bson;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using AutoMapper;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Api.Models
{
	[AutoMap(typeof(UserModel))]
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToUserUpdate")]
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToAdminUpdate")]
	public class ApiUser
	{
		public ObjectId Id { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string Username { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string Email { get; set; }

		[UpdateProperty(MethodName = "ToAdminUpdate")]
		public bool IsLockoutEnabled { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string FirstName { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string LastName { get; set; }

		[UpdateProperty(ApplyToAllMethods = true)]
		public string AvatarUrl { get; set; }

		[UpdatePropertyIgnore]
		public ApiGroup[] Groups { get; set; }

		[UpdatePropertyIgnore]
		public string[] Permissions { get; set; }
	}
}
