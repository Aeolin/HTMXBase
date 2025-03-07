using AutoMapper;
using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Api.Models
{
	[AutoMap(typeof(GroupModel))]
	public class ApiGroup
	{
		public required string Slug { get; set; }
		public required string Name { get; set; }
		public string? Description { get; set; }
		public string[]? Permissions { get; set; }
	}
}
