using System.ComponentModel.DataAnnotations;

namespace MongoDBSemesterProjekt.Models
{
	public class GroupModel : EntityBase
	{
		public const string CollectionName = "groups";

		public required string Slug { get; set; }
		public required string Name { get; set; }
		public string? Description { get; set; }
		public List<string> Permissions { get; set; } = new List<string>();
	}
}
