using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MongoDBSemesterProjekt.Models
{
	public class GroupModel : EntityBase
	{
		public const string CollectionName = "groups";

		public required string Slug { get; set; }
		public required string Name { get; set; }
		public string? Description { get; set; }
		public IList<string> Permissions { get; set; } = new List<string>();
	}
}
