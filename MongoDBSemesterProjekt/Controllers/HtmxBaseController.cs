using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Services.TemplateStore;
using MongoDBSemesterProjekt.Utils;

namespace MongoDBSemesterProjekt.Controllers
{
	public abstract class HtmxBaseController : ControllerBase
	{
		protected readonly IMongoDatabase _db;
		protected readonly IMapper _mapper;

		protected static readonly Dictionary<Type, object> _cachedUpdateOptions = new Dictionary<Type, object>();
		protected static FindOneAndUpdateOptions<T> GetReturnUpdatedOptions<T>() => (FindOneAndUpdateOptions<T>)_cachedUpdateOptions.GetOrAdd(typeof(T), () => new FindOneAndUpdateOptions<T> { ReturnDocument = ReturnDocument.After });


		protected HtmxBaseController(IMongoDatabase dataBase, IMapper mapper)
		{
			_db = dataBase;
			_mapper = mapper;
		}
	}
}