using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using HTMXBase.Database.Models;
using HTMXBase.Services.TemplateStore;
using HTMXBase.Utils;

namespace HTMXBase.Controllers
{
	public abstract class HtmxBaseController : ControllerBase
	{
		protected readonly IMongoDatabase _db;
		protected readonly IMapper _mapper;

		protected static readonly Dictionary<Type, object> _cachedUpdateOptions = new Dictionary<Type, object>();
		protected static FindOneAndUpdateOptions<T> GetReturnUpdatedOptions<T>() => (FindOneAndUpdateOptions<T>)_cachedUpdateOptions.GetOrAdd(typeof(T), () => new FindOneAndUpdateOptions<T> { ReturnDocument = ReturnDocument.After });

		protected async Task<UserModel?> GetUserAsync()
		{
			var userId = User?.GetIdentifierId();
			var user = await _db.GetCollection<UserModel>(UserModel.CollectionName).Find(x => x.Id == userId).FirstOrDefaultAsync(HttpContext.RequestAborted);
			if (user == null)
				return null;

			return user;
		}

		protected IActionResult NoPermission(string reason) => StatusCode(StatusCodes.Status403Forbidden, reason); 

		protected HtmxBaseController(IMongoDatabase dataBase, IMapper mapper)
		{
			_db = dataBase;
			_mapper = mapper;
		}
	}
}