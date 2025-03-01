using MongoDB.Bson;

namespace MongoDBSemesterProjekt.ApiModels
{
	public class ApiUser
	{
		public string Username { get; set; }
		public string Email { get; set; }

		public bool IsLockoutEnabled { get; set; }

		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string AvatarUrl { get; set; }
	}
}
