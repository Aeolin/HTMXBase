using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Authorization;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Services.FileStorage;
using MongoDBSemesterProjekt.Utils;
using NSwag;
using System.Security.Claims;
using System.Web;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("/api/v1/files")]
	[Authorize]
	public class StaticContentController : ControllerBase
	{
		private readonly IMongoDatabase _db;
		private readonly IFileStorage _fileStore;

		public StaticContentController(IMongoDatabase db, IFileStorage fileStore)
		{
			_db=db;
			_fileStore=fileStore;
		}

		[HttpPost]
		[ProducesResponseType<string>(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Permission("files/upload", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> UploadFileAsync(IFormFile file, [FromForm] string? slug = null, [FromForm] string? virtualPath = null, [FromForm] string? deletePermission = null, [FromForm] string? readPermission = null)
		{
			slug ??= HttpUtility.UrlEncode(file.Name.Replace(" ", "-"));
			var collection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			var ownerId = User.GetIdentifierId();

			var path = await _fileStore.StoreFileAsync(file);
			var content = new StaticContentModel
			{
				OwnerId = ownerId.Value,
				Length = file.Length,
				MimeType = file.ContentType,
				Name = file.Name,
				StorageId = path,
				VirtualPath = virtualPath,
				Slug = slug,
				DeletePermission = deletePermission,
				ReadPermission = readPermission
			};

			await collection.InsertOneAsync(content);
			return Ok(content.Id.ToString());
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[Permission("files/delete", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> DeleteFileAsync(ObjectId id)
		{
			var collection = _db.GetCollection<StaticContentModel>(StaticContentModel.CollectionName);
			var ownerId = User.GetIdentifierId();

			var permissions = User.FindAll(Constants.PERMISSION_CLAIM).Select(x => x.Value).ToArray();
			var result = await collection.FindOneAndDeleteAsync(x => x.Id == id && (x.OwnerId == ownerId || (x.DeletePermission != null && permissions.Contains(x.DeletePermission))));
			if (result == null)
				return NotFound();

			await _fileStore.DeleteFileAsync(result.StorageId);
			return Ok();
		}
	}
}
