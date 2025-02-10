namespace MongoDBSemesterProjekt.ApiModels
{
	public class LoginRequest
	{
		public required string UsernameOrEmail { get; set; }
		public required string Password { get; set; }
	}
}
