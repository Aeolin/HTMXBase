
using System.Web;

namespace MongoDBSemesterProjekt.Middleware
{
	public class RedirectHandlingMiddleware : IMiddleware
	{
		public async Task InvokeAsync(HttpContext context, RequestDelegate next)
		{
			await next(context);
			if (context.Request.Query.TryGetValue("redirect", out var redirect) && context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
			{
				context.Response.StatusCode = 302;
				context.Response.Headers["Location"] = redirect;
			}
		}
	}
}
