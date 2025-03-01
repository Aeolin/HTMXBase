using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDBSemesterProjekt.Services.TemplateStore;

namespace MongoDBSemesterProjekt.Controllers
{
	public abstract class HtmxBaseController : ControllerBase
	{
		protected readonly IMongoDatabase _db;
		protected readonly IMapper _mapper;

		protected HtmxBaseController(IMongoDatabase dataBase, IMapper mapper)
		{
			_db = dataBase;
			_mapper = mapper;
		}
	}
}