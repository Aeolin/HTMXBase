using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using HTMXBase.Api.Models;
using HTMXBase.Authorization;
using HTMXBase.Database;
using HTMXBase.Database.Models;
using HTMXBase.Services.FileStorage;
using HTMXBase.Utils;
using NSwag;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Web;

namespace HTMXBase.Controllers
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

		[HttpGet("dir")]
		[ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
		[Permission("files/read", Constants.BACKEND_USER, Constants.ADMIN_ROLE)]
		public async Task<IActionResult> GetDirectoryAsync([FromQuery] string? path = null)
		{
			path ??= string.Empty;
			if(path.EndsWith('/') == false)
				path+='/';

			var escaped = path.Replace("/", "\\/");
			var startsWith = Builders<StaticContentModel>.Filter.Regex(x => x.VirtualPath, new BsonRegularExpression($@"^{escaped}(([^\/]+$)|([^\/]+\/([^\/]+$)))"));
			var files = await _db.GetCollection<StaticContentModel>().Find(startsWith).ToListAsync();
			var list = new List<ApiFile>();
			var dirSet = new HashSet<string>();

			foreach (var file in files)
			{
				var vpath = file.VirtualPath ?? string.Empty;
				var nextDir = vpath.IndexOf('/', path.Length + 1);
				if (nextDir == -1)
				{
					list.Add(new ApiFile
					{
						Name = vpath.Substring(path.Length),
						MimeType = file.MimeType,
						Type = ApiFileType.File
					});
				}
				else
				{
					var dir = vpath.Substring(path.Length, nextDir - path.Length);
					if (dirSet.Add(dir))
					{
						list.Add(new ApiFile
						{
							Name = dir,
							Type = ApiFileType.Directory
						});
					}		
				}
			}

			return Ok(dirSet.ToArray());
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
				Name = file.FileName,
				StorageId = path,
				VirtualPath = virtualPath ?? "/",
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

			var permissions = User.GetPermissions();
			var result = await collection.FindOneAndDeleteAsync(x => x.Id == id && (x.OwnerId == ownerId || (x.DeletePermission != null && permissions.Contains(x.DeletePermission))));
			if (result == null)
				return NotFound();

			await _fileStore.DeleteFileAsync(result.StorageId);
			return Ok();
		}
	}
}
