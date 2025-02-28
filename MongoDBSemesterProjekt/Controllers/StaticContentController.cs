using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Models;
using MongoDBSemesterProjekt.Services.FileStorage;
using MongoDBSemesterProjekt.Utils;
using NSwag;
using System.Security.Claims;
using System.Web;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("/files")]
	public class StaticContentController : ControllerBase
	{
		private readonly IMongoDatabase _db;
		private readonly IFileStorage _fileStore;

		public StaticContentController(IMongoDatabase db, IFileStorage fileStore)
		{
			_db=db;
			_fileStore=fileStore;
		}

		[Authorize]
		[HttpPost]
		[ProducesResponseType<string>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> UploadFileAsync(IFormFile file, [FromForm] string? slug = null, [FromForm] string? virtualPath = null, [FromForm] string? deletePermission = null, [FromForm] string? readPermission = null)
		{
			slug ??= HttpUtility.UrlEncode(file.Name.Replace(" ", "-"));
			var collection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			if ((await collection.CountDocumentsAsync(x => x.Slug == slug && x.VirtualPath == virtualPath)) > 0)
				return BadRequest("File with Slug and VirtualPath already exists");

			var owner = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (ObjectId.TryParse(owner, out var ownerId) == false)
				return BadRequest("Invalid Owner");

			var path = await _fileStore.StoreFileAsync(file);
			var content = new StaticContentModel
			{
				OwnerId = ownerId,
				Length = file.Length,
				MimeType = file.ContentType,
				Name = file.Name,
				Path = path,
				VirtualPath = virtualPath,
				Slug = slug,
				DeletePermission = deletePermission,
				ReadPermission = readPermission
			};

			await collection.InsertOneAsync(content);
			return Ok(content.Id.ToString());
		}

		[Authorize]
		[HttpDelete("{id}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public async Task<IActionResult> DeleteFileAsync(ObjectId id)
		{
			var collection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			var owner = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (ObjectId.TryParse(owner, out var ownerId) == false)
				return StatusCode(StatusCodes.Status500InternalServerError, "Expected id to be ObjectId");

			var permissions = User.FindAll(Constants.PERMISSION_CLAIM).Select(x => x.Value).ToArray();
			var result = await collection.FindOneAndDeleteAsync(x => x.Id == id && (x.OwnerId == ownerId || (x.DeletePermission != null && permissions.Contains(x.DeletePermission))));
			if (result == null)
				return NotFound();

			await _fileStore.DeleteFileAsync(result.Path);
			return Ok();
		}

		[AllowAnonymous]
		[HttpGet("{**path}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> GetFileContentAsync(string path)
		{
			var collection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			var lastSlash = path.LastIndexOf('/');
			var slug = path.Substring(lastSlash + 1);
			var virtualPath = lastSlash == -1 ? null : path.Substring(0, lastSlash);

			StaticContentModel model = null;
			if (User == null)
			{
				var files = await collection.FindAsync(x => x.Slug == slug && x.VirtualPath == virtualPath);
				model = await files.FirstOrDefaultAsync();	
			}
			else
			{
				var owner = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (ObjectId.TryParse(owner, out var ownerId) == false)
					return StatusCode(StatusCodes.Status500InternalServerError, "Expected id to be ObjectId");

				var permissions = User.FindAll(Constants.PERMISSION_CLAIM).Select(x => x.Value).ToArray();
				var result = await collection.FindAsync(
					x => x.Slug == slug &&
					x.VirtualPath == virtualPath &&
					(x.OwnerId == ownerId || x.ReadPermission == null || permissions.Contains(x.ReadPermission)) 
				);
			}

			if (model == null)
				return NotFound();

			var stream = await _fileStore.GetBlobAsync(model.Path);
			if (stream == null)
				return NotFound();

			return File(stream, model.MimeType, model.Name);
		}
	}
}
