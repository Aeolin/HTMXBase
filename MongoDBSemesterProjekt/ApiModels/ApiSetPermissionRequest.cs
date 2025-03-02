﻿using MongoDBSemesterProjekt.Models;
using AwosFramework.Generators.MongoDBUpdateGenerator;

namespace MongoDBSemesterProjekt.ApiModels
{

	[MongoDBUpdate(typeof(GroupModel), MethodName = "ToGroupAddPermission")]
	[MongoDBUpdate(typeof(GroupModel), MethodName = "ToGroupRemovePermission")]
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToUserAddPermission")]
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToUserRemovePermission")]
	public class ApiSetPermissionRequest
	{
		[UpdateProperty(MethodName = "ToGroupAddPermission", CollectionHandling = CollectionHandling.AddToSet)]
		[UpdateProperty(MethodName = "ToGroupRemovePermission", CollectionHandling = CollectionHandling.PullAll)]
		[UpdateProperty(MethodName = "ToUserAddPermission", CollectionHandling = CollectionHandling.AddToSet)]
		[UpdateProperty(MethodName = "ToUserRemovePermission", CollectionHandling = CollectionHandling.PullAll)]
		public string[]? Permissions { get; set; }
	}
}
