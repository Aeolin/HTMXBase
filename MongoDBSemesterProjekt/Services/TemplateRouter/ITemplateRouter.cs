namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public interface ITemplateRouter
	{
		public bool TryRoute(Uri route, out RouteMatch match);
	}
}
