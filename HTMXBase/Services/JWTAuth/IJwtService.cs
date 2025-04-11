using HTMXBase.Database.Models;
using System.Security.Claims;

namespace HTMXBase.Services.JWTAuth
{
	public interface IJwtService
	{
		public Task<ClaimsIdentity> BuildIdentityAsync(UserModel model);
	}
}
