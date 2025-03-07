using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Authorization;
using MongoDBSemesterProjekt.Utils;
using AwosFramework.Generators.MongoDBUpdateGenerator.Extensions;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Api.Models;
using MongoDBSemesterProjekt.DataBinders.JsonOrForm;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("/api/v1/user")]
	[Authorize]
	public class UserController : HtmxBaseController
	{
		public UserController(IMongoDatabase dataBase, IMapper mapper) : base(dataBase, mapper)
		{
		}

		[HttpGet("self")]
		[ProducesResponseType<ApiUser>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("user/get-self", Constants.USER_ROLE)]
		public async Task<IActionResult> GetCurrentUserAsync()
		{
			var id = User.GetIdentifierId();
			var user = await _db.GetCollection<UserModel>(UserModel.CollectionName).Find(x => x.Id == id).FirstOrDefaultAsync();

			if (user == null)
				return NotFound();

			return Ok(_mapper.Map<ApiUser>(user));
		}

		[HttpPut("self")]
		[ProducesResponseType<ApiUser>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("user/modify-self", Constants.USER_ROLE)]
		public async Task<IActionResult> UpdateCurrentUserAsync([FromJsonOrForm] ApiUser user)
		{
			var id = User.GetIdentifierId();
			var userModel = await _db.GetCollection<UserModel>(UserModel.CollectionName)
				.FindOneAndUpdateAsync(x => x.Id == id, user.ToUserUpdate(), GetReturnUpdatedOptions<UserModel>());

			if(userModel == null)
				return NotFound();

			return Ok(_mapper.Map<ApiUser>(userModel));
		}
	}
}
