using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.ApiModels.Requests;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.DataBinders.JsonOrForm;
using MongoDBSemesterProjekt.Services.JWTAuth;
using MongoDBSemesterProjekt.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("/api/v1/auth")]
	[Authorize(Roles = Constants.USER_ROLE)]
	public class AuthController : ControllerBase
	{
		private readonly IMongoDatabase _db;
		private readonly PasswordHasher<UserModel> _hasher;
		private readonly IJwtService _jwtService;
		private readonly JwtOptions _jwtOpts;
		private readonly SigningCredentials _signingCredentials;
		private readonly JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();

		public AuthController(IMongoDatabase db, PasswordHasher<UserModel> hasher, JwtOptions jwtOpts, IJwtService jwtService)
		{
			_db=db;
			_hasher=hasher;
			_jwtOpts=jwtOpts;
			_jwtService=jwtService;
			var key = Encoding.UTF8.GetBytes(_jwtOpts.PrivateKey);
			_signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
		}

		private string GetGravatarUrl(string email)
		{
			var hash = MD5.HashData(Encoding.UTF8.GetBytes(email.ToLower()));
			var hex = string.Join("", hash.Select(x => x.ToString("x2")));
			return $"https://www.gravatar.com/avatar/{hex}";
		}

		[HttpPost("register")]
		[AllowAnonymous]
		public async Task<IActionResult> RegisterAsync([FromJsonOrForm] RegisterRequest request)
		{
			var user = new UserModel
			{
				Email = request.Email,
				Username = request.Username ?? request.Email.ToLower(),
				FirstName = request.FirstName,
				LastName = request.LastName,
				PasswordHash = "",
				AvatarUrl = request.AvatarUrl ?? GetGravatarUrl(request.Email),
				Groups = [Constants.USER_ROLE]
			};

			var users = _db.GetCollection<UserModel>(UserModel.CollectionName);
			var count = await users.CountDocumentsAsync(FilterDefinition<UserModel>.Empty);
			if (count == 0) // first registered user will become admin for sake of simplicity
			{
				user.Groups.Add(Constants.ADMIN_ROLE);
				user.Groups.Add(Constants.BACKEND_USER);
			}

			user.PasswordHash = _hasher.HashPassword(user, request.Password);
			await users.InsertOneAsync(user);
			return Ok();
		}

		private SecurityTokenDescriptor BuildDescriptor(ClaimsIdentity identity)
		{
			return new SecurityTokenDescriptor
			{
				Expires = _jwtOpts.Expires.HasValue ? DateTime.UtcNow + _jwtOpts.Expires.Value : null,
				Subject = identity,
				IssuedAt = DateTime.UtcNow,
				Issuer = _jwtOpts.Issuer,
				Audience = _jwtOpts.Audience,
				NotBefore = DateTime.UtcNow,
				SigningCredentials = _signingCredentials
			};
		}

		[HttpPost("login")]
		[AllowAnonymous]
		public async Task<IActionResult> LoginAsync([FromJsonOrForm] LoginRequest request, [FromQuery] bool useCookie = false)
		{
			var lowerEmailOrUser = request.UsernameOrEmail.ToLower();
			var user = _db.GetCollection<UserModel>(UserModel.CollectionName).Find(x => x.Username == lowerEmailOrUser || x.Email == lowerEmailOrUser).FirstOrDefault();
			if (user.IsLockoutEnabled || _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
				return Unauthorized();

			var identity = await _jwtService.BuildIdentityAsync(user);
			var descriptor = BuildDescriptor(identity);

			var token = _handler.CreateToken(descriptor);
			var written = _handler.WriteToken(token);

			if (useCookie)
			{
				HttpContext.Response.Cookies.Append("jwt", written);
				return Ok();
			}
			else
			{
				return Ok(written);
			}
		}

		[HttpGet("logout")]
		public IActionResult LogoutAsync()
		{
			HttpContext.Response.Cookies.Delete("jwt");
			return Ok();
		}


		[HttpPost("refresh-token")]
		public async Task<IActionResult> RefreshTokenAsync([FromQuery] bool useCookie = false)
		{
			var id = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
			var user = await _db.GetCollection<UserModel>(UserModel.CollectionName)
				.Find(Builders<UserModel>.Filter.Eq("_id", id))
				.FirstOrDefaultAsync();

			if (user == null || user.IsLockoutEnabled)
				return Unauthorized();

			var identity = await _jwtService.BuildIdentityAsync(user);
			var descriptor = BuildDescriptor(identity);
			var token = _handler.CreateToken(descriptor);
			var written = _handler.WriteToken(token);

			if (useCookie)
			{
				HttpContext.Response.Cookies.Append("jwt", written);
				return Ok();
			}
			else
			{
				return Ok(written);
			}
		}

	}
}
