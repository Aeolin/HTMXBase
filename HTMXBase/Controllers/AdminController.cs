using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using HTMXBase.ApiModels;
using HTMXBase.Authorization;
using HTMXBase.Utils;
using System.ComponentModel.DataAnnotations;
using AwosFramework.Generators.MongoDBUpdateGenerator.Extensions;
using HTMXBase.ApiModels.Requests;
using HTMXBase.Database.Models;
using HTMXBase.Api.Models;
using HTMXBase.DataBinders.JsonOrForm;
using HTMXBase.Services.Pagination;

namespace HTMXBase.Controllers
{
	[ApiController]
	[Route("api/v1/admin")]
	public class AdminController : HtmxBaseController
	{
	
		private readonly IPaginationService<UserModel> _paginationService;

		public AdminController(IMongoDatabase dataBase, IMapper mapper, IPaginationService<UserModel> paginationService) : base(dataBase, mapper)
		{
			_paginationService=paginationService;
		}
	
		private async IAsyncEnumerable<ApiUser> ToApiUserAsync(IEnumerable<UserModel> users)
		{
			var groups = users.SelectMany(x => x.Groups).Distinct();
			var groupModels = await _db.GetCollection<GroupModel>(GroupModel.CollectionName).Find(x => groups.Contains(x.Slug)).ToListAsync();
			var groupLookup = groupModels.ToDictionary(x => x.Slug, x => _mapper.Map<ApiGroup>(x));
			foreach (var user in users)
			{
				var apiUser = _mapper.Map<ApiUser>(user);
				apiUser.Groups = user.Groups.Select(x => groupLookup[x]).ToArray();
				yield return apiUser;
			}
		}

		private async Task<ApiUser> ToApiUserAsync(UserModel user)
		{
			var groups = user.Groups.Distinct();
			var groupModels = await _db.GetCollection<GroupModel>(GroupModel.CollectionName).Find(x => groups.Contains(x.Slug)).ToListAsync();
			var apiUser = _mapper.Map<ApiUser>(user);
			apiUser.Groups = user.Groups.Select(x => _mapper.Map<ApiGroup>(x)).ToArray();
			return apiUser;
		}

		[HttpGet("groups")]
		[ProducesResponseType<ObjectIdCursorResult<ApiGroup[]>>(StatusCodes.Status200OK)]
		[Permission("admin/get-group", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> ListGroupsAsync([FromQuery][Range(1, 100)]int limit = 20, [FromQuery]ObjectId? cursorNext = null, [FromQuery]ObjectId? cursorPrevious = null) 
		{
			var data = await _db.GetCollection<GroupModel>(GroupModel.CollectionName)
				.PaginateAsync(limit, cursorNext, cursorPrevious, x => x.Id, _mapper.Map<ApiGroup>);

			return Ok(data);
		}

		[HttpPost("groups")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Permission("admin/create-group", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> CreateGroupAsync([FromJsonOrForm] ApiGroup group)
		{
			var model = _mapper.Map<GroupModel>(group);
			await _db.GetCollection<GroupModel>(GroupModel.CollectionName).InsertOneAsync(model);
			return Ok(_mapper.Map<ApiGroup>(model));
		}

		[HttpDelete("groups/{slug}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/delete-group", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> DeleteGroupAsync([FromRoute] string slug)
		{
			var result = await _db.GetCollection<GroupModel>(GroupModel.CollectionName).DeleteOneAsync(x => x.Slug == slug);
			if (result.DeletedCount == 0)
				return NotFound();

			return Ok();
		}

		[HttpPut("groups/{slug}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/update-group", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> UpdateGroupAsync([FromRoute] string slug, [FromJsonOrForm] ApiGroupUpdateRequest update)
		{	
			var result = await _db.GetCollection<GroupModel>(GroupModel.CollectionName)
				.FindOneAndUpdateAsync(x => x.Slug == slug, update.ToUpdate(), GetReturnUpdatedOptions<GroupModel>());

			if (result == null)
				return NotFound();
			
			return Ok(_mapper.Map<ApiGroup>(result));
		}

		[HttpGet("groups/{slug}")]
		[ProducesResponseType<ApiGroup>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/get-group", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> GetGroupAsync([FromRoute] string slug)
		{
			var group = await _db.GetCollection<GroupModel>(GroupModel.CollectionName).Find(x => x.Slug == slug).FirstOrDefaultAsync();
			if (group == null)
				return NotFound();

			return Ok(_mapper.Map<ApiGroup>(group));
		}

		[HttpPut("groups/{slug}/permissions")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/update-group", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> UpdateGroupPermissionsAsync([FromRoute] string slug, [FromJsonOrForm] ApiSetPermissionRequest permissions)
		{
			var update = permissions.ToGroupAddPermission();
			var result = await _db.GetCollection<GroupModel>(GroupModel.CollectionName)
				.FindOneAndUpdateAsync(x => x.Slug == slug, update, GetReturnUpdatedOptions<GroupModel>());
			
			if (result == null)
				return NotFound();
			
			return Ok(_mapper.Map<ApiGroup>(result));
		}

		[HttpDelete("groups/{slug}/permissions")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/update-group", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> RemoveGroupPermissionsAsync([FromRoute] string slug, [FromJsonOrForm] ApiSetPermissionRequest permissions)
		{
			var result = await _db.GetCollection<GroupModel>(GroupModel.CollectionName)
				.FindOneAndUpdateAsync(x => x.Slug == slug, permissions.ToGroupRemovePermission(), GetReturnUpdatedOptions<GroupModel>());
			
			if (result == null)
				return NotFound();
			
			return Ok(_mapper.Map<ApiGroup>(result));
		}

		[HttpGet("groups/{slug}/permissions")]
		[ProducesResponseType<string[]>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/get-group", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> GetGroupPermissionsAsync([FromRoute] string slug)
		{
			var group = await _db.GetCollection<GroupModel>(GroupModel.CollectionName).Find(x => x.Slug == slug).FirstOrDefaultAsync();
			if (group == null)
				return NotFound();

			return Ok(group.Permissions);
		}

		[HttpGet("users")]
		[ProducesResponseType<ObjectIdCursorResult<ApiUser[]>>(StatusCodes.Status200OK)]
		[Permission("admin/get-user", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> ListUsersAsync()
		{
			var paginationValues = PaginationValues.FromRequest(HttpContext);
			var data = await _paginationService.PaginateAsync<ApiUser>(paginationValues, ToApiUserAsync);
			return Ok(data);
		}

		[HttpGet("users/search")]
		[ProducesResponseType<ObjectIdCursorResult<ApiUser[]>>(StatusCodes.Status200OK)]
		[Permission("admin/get-user", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> FindUsersAsync([FromQuery]string? name = null, [FromQuery]string? email = null)
		{
			var b = Builders<UserModel>.Filter;
			var list = new List<FilterDefinition<UserModel>>();
			if(string.IsNullOrEmpty(name) == false)
			{
				list.Add(b.Regex(x => x.FirstName, name));
				list.Add(b.Regex(x => x.LastName, name));
				list.Add(b.Regex(x => x.Username, name));
			}

			if (string.IsNullOrEmpty(email) == false)
				list.Add(b.Regex(x => x.Email, email));

			var filter = list.Count > 1 ? b.Or(list) : list.FirstOrDefault();
			var paginationValues = PaginationValues.FromRequest(HttpContext);
			var users = await _paginationService.PaginateAsync<ApiUser>(paginationValues, ToApiUserAsync, filter);
			return Ok(users);
		}

		[HttpGet("users/{id}")]
		[ProducesResponseType<ApiUser>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/get-user", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> GetUserAsync([FromRoute] ObjectId id)
		{
			var user = await _db.GetCollection<UserModel>(UserModel.CollectionName).Find(x => x.Id == id).FirstOrDefaultAsync();
			if (user == null)
				return NotFound();
			
			var api = await ToApiUserAsync(user);
			return Ok(api);
		}

		[HttpPut("users/{id}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/update-user", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> UpdateUserAsync([FromRoute] ObjectId id, [FromJsonOrForm] ApiUser update)
		{
			var result = await _db.GetCollection<UserModel>(UserModel.CollectionName)
				.FindOneAndUpdateAsync(x => x.Id == id, update.ToAdminUpdate(), GetReturnUpdatedOptions<UserModel>());
			
			if (result == null)
				return NotFound();
			
			var api = await ToApiUserAsync(result);
			return Ok(api);
		}

		[HttpGet("users/{id}/groups")]
		[ProducesResponseType<ApiGroup[]>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/get-user", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> GetUserGroupsAsync([FromRoute] ObjectId id)
		{
			var user = await _db.GetCollection<UserModel>(UserModel.CollectionName).Find(x => x.Id == id).FirstOrDefaultAsync();
			if (user == null)
				return NotFound();
			
			var groups = await _db.GetCollection<GroupModel>(GroupModel.CollectionName).Find(x => user.Groups.Contains(x.Slug)).ToListAsync();
			return Ok(_mapper.Map<ApiGroup[]>(groups));
		}

		[HttpPut("users/{id}/groups")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/update-user", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> UpdateUserGroupsAsync([FromRoute] ObjectId id, [FromJsonOrForm] ApiSetGroupRequest groups)
		{
			var result = await _db.GetCollection<UserModel>(UserModel.CollectionName)
				.FindOneAndUpdateAsync(x => x.Id == id, groups.ToUserAddGroup(), GetReturnUpdatedOptions<UserModel>());

			if (result == null)
				return NotFound();

			var api = await ToApiUserAsync(result);
			return Ok(api);
		}

		[HttpDelete("users/{id}/groups")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Permission("admin/update-user", Constants.ADMIN_ROLE)]
		public async Task<IActionResult> RemoveUserGroupsAsync([FromRoute] ObjectId id, [FromJsonOrForm] ApiSetGroupRequest groups)
		{
			var result = await _db.GetCollection<UserModel>(UserModel.CollectionName)
				.FindOneAndUpdateAsync(x => x.Id == id, groups.ToUserRemoveGroup(), GetReturnUpdatedOptions<UserModel>());
		
			if (result == null)
				return NotFound();

			var api = await ToApiUserAsync(result);
			return Ok(api);
		}
	}
}
