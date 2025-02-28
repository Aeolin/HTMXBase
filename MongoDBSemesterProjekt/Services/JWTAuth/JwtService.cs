using MongoDB.Driver;
using MongoDBSemesterProjekt.Models;
using MongoDBSemesterProjekt.Utils;
using System.Security.Claims;

namespace MongoDBSemesterProjekt.Services.JWTAuth
{
	public class JwtService : IJwtService
	{
		private readonly IMongoDatabase _db;

		public JwtService(IMongoDatabase db)
		{
			_db=db;
		}

		public async Task<ClaimsIdentity> BuildIdentityAsync(UserModel model)
		{
			var identity = new ClaimsIdentity();
			identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, model.Id.ToString()));
			identity.AddClaim(new Claim(ClaimTypes.Name, model.Username));
			identity.AddClaim(new Claim(ClaimTypes.Email, model.Email));
			if (string.IsNullOrEmpty(model.FirstName) == false && string.IsNullOrEmpty(model.LastName) == false)
				identity.AddClaim(new Claim(ClaimTypes.GivenName, $"{model.FirstName} {model.LastName}"));

			foreach (var role in model.Groups)
				identity.AddClaim(new Claim(ClaimTypes.Role, role));

			var groups = await _db.GetCollection<GroupModel>(GroupModel.CollectionName).Find(x => model.Groups.Contains(x.Slug)).Project(x => x.Permissions).ToListAsync();
			var permission = groups.SelectMany(x => x).Concat(model.Permissions).Distinct();
			foreach (var perm in permission)
				identity.AddClaim(new Claim(Constants.PERMISSION_CLAIM, perm));


			return identity;
		}
	}
}
