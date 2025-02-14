using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ThirdParty.BouncyCastle.Asn1;

namespace MongoDBSemesterProjekt.Controllers
{
	[ApiController]
	[Route("/v1/collections")]
	public class CollectionController : Controller
	{
		[HttpGet("{collectionId}/query")]
		public async Task<IActionResult> QueryAsync(string collectionId, [FromQuery][FromForm]int offset = 0, [FromQuery][FromForm]int limit=20, [FromQuery][FromForm]JsonDocument filter = null)
		{
			return Ok();
		}


	}
}
