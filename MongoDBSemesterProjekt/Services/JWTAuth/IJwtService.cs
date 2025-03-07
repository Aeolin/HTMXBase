using MongoDBSemesterProjekt.Database.Models;
using System.Security.Claims;

namespace MongoDBSemesterProjekt.Services.JWTAuth
{
	public interface IJwtService
	{
		public Task<ClaimsIdentity> BuildIdentityAsync(UserModel model);
	}
}
