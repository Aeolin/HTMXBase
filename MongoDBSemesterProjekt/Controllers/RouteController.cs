using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("api/v1/routes")]
	public class RouteController : HtmxBaseController
	{
		public RouteController(IMongoDatabase dataBase, IMapper mapper) : base(dataBase, mapper)
		{
		}
	}
}
