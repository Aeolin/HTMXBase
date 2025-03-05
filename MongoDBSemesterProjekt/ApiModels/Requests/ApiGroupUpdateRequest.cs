﻿using MongoDBSemesterProjekt.Models;
using AwosFramework.Generators.MongoDBUpdateGenerator;

namespace MongoDBSemesterProjekt.ApiModels.Requests
{
	[MongoDBUpdate(typeof(GroupModel))]
	public class ApiGroupUpdateRequest
	{
		public string? Slug { get; set; }
		public string? Name { get; set; }
		public string? Description { get; set; }
		public string[]? Permissions { get; set; }
	}
}
