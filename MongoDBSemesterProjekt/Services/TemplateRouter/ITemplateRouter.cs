namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public interface ITemplateRouter
	{
		public bool TryRoute(HttpContext context, out RouteMatch match);
	}
}
