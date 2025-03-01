using MongoDB.Driver;
using MongoDBSemesterProjekt.ApiModels;
using MongoDBSemesterProjekt.Models;

namespace MongoDBSemesterProjekt.Utils
{
	public static class ApiExtensions
	{
		public static UpdateDefinition<GroupModel> ToUpdate(this ApiGroupUpdateRequest req)
		{
			var builder = Builders<GroupModel>.Update;
			var list = new List<UpdateDefinition<GroupModel>>();

			if (string.IsNullOrEmpty(req.Slug) == false)
				list.Add(builder.Set(x => x.Slug, req.Slug));

			if (string.IsNullOrEmpty(req.Name) == false)
				list.Add(builder.Set(x => x.Name, req.Name));

			if (string.IsNullOrEmpty(req.Description) == false)
				list.Add(builder.Set(x => x.Description, req.Description));

			if (req.Permissions != null && req.Permissions.Length > 0)
				list.Add(builder.Set(x => x.Permissions, req.Permissions));

			return builder.Combine(list);
		}
	}
}
