using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Models;
using MongoDBSemesterProjekt.Utils;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("/api/v1/groups")]
	[Authorize(Roles = Constants.ADMIN_ROLE)]
	public class GroupController : ControllerBase
	{
		private readonly IMongoDatabase _db;

		public GroupController(IMongoDatabase db)
		{
			_db=db;
		}

		[HttpGet("{groupSlug}/users")]
		public async Task<IActionResult> GetUsersAsync(string groupId)
		{
			var id = ObjectId.Parse(groupId);
			var users = await _db.GetCollection<UserModel>(UserModel.CollectionName).Find(x => x.Groups.Contains(id)).ToListAsync();
			return Ok(users);
		}

		[HttpGet]
		public async Task<IActionResult> GetGroupsAsync([FromForm][FromQuery]int offset = 0, [FromForm][FromQuery]int limit = 20)
		{
			var groups = await _db.GetCollection<GroupModel>(GroupModel.CollectionName).Find(_ => true).Skip(offset).Limit(limit).ToListAsync();
			return Ok(groups);
		}
	}
}
