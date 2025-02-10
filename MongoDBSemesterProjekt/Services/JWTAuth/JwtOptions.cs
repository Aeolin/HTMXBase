namespace MongoDBSemesterProjekt.Services.JWTAuth
{
	public class JwtOptions
	{
		public required string PrivateKey { get; set; }
		public required string Issuer { get; set; }
		public required string Audience { get; set; }
		public TimeSpan? Expires { get; set; }
	}
}
