using MongoDBSemesterProjekt.Models;
using AwosFramework.Generators.MongoDBUpdateGenerator;

namespace MongoDBSemesterProjekt.ApiModels
{
	public class ApiGroup
	{
		public required string Slug { get; set; }
		public required string Name { get; set; }
		public string? Description { get; set; }
		public string[]? Permissions { get; set; }
	}

	[MongoDBUpdate(typeof(GroupModel))]
	public class ApiGroupUpdateRequest
	{
		public string? Slug { get; set; }
		public string? Name { get; set; }
		public string? Description { get; set; }
		public string[]? Permissions { get; set; }
	}


	[MongoDBUpdate(typeof(GroupModel), MethodName = "ToUpdateAddPermission")]
	[MongoDBUpdate(typeof(GroupModel), MethodName = "ToUpdateRemovePermission")]
	public class ApiGroupSetPermissionRequest
	{
		[UpdateProperty(MethodName = "ToUpdateAddPermission", CollectionHandling = CollectionHandling.AddToSet)]
		[UpdateProperty(MethodName = "ToUpdateRemovePermission", CollectionHandling = CollectionHandling.PullAll)]
		public string[]? Permissions { get; set; }
	}
}
