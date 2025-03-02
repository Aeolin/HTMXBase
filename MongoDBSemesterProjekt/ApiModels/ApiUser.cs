using MongoDB.Bson;
using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDBSemesterProjekt.Models;

namespace MongoDBSemesterProjekt.ApiModels
{
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToUserUpdate")]
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToAdminUpdate")]
	public class ApiUser
	{
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
	}
}
