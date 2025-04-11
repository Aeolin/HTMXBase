using AwosFramework.Generators.MongoDBUpdateGenerator;
using HTMXBase.Database.Models;

namespace HTMXBase.ApiModels.Requests
{
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToUserAddGroup")]
	[MongoDBUpdate(typeof(UserModel), MethodName = "ToUserRemoveGroup")]
	public class ApiSetGroupRequest
	{
		[UpdateProperty(MethodName = "ToUserAddGroup", CollectionHandling = CollectionHandling.AddToSet)]
		[UpdateProperty(MethodName = "ToUserRemoveGroup", CollectionHandling = CollectionHandling.PullAll)]
		public string[] Groups { get; set; }
	}
}
