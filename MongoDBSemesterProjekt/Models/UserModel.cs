using MongoDB.Bson;

namespace MongoDBSemesterProjekt.Models
{
	public class UserModel : EntityBase
	{
		public const string CollectionName = "users";

		public required string PasswordHash { get; set; }
		public required string Username { get; set; }
		public required string Email { get; set; }

		public bool IsLockoutEnabled { get; set; } = false;

		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? AvatarUrl { get; set; }

		public IList<string> Roles { get; set; } = new List<string>();
		public IList<ObjectId> Groups { get; set; } = new List<ObjectId>();
	}
}
