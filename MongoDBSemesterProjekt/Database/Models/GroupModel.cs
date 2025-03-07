using AwosFramework.Generators.MongoDBUpdateGenerator;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MongoDBSemesterProjekt.Database.Models
{

	[MongoDBUpdate(typeof(GroupModel))]
	public class GroupModel : EntityBase
	{
		public const string CollectionName = "groups";

		[Index(IsUnique = true)]
		public required string Slug { get; set; }		
		public required string Name { get; set; }	
		public string? Description { get; set; }

		[UpdateProperty(CollectionHandling = CollectionHandling.Set)]
		public IList<string> Permissions { get; set; } = new List<string>();
	}
}
