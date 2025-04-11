namespace HTMXBase.Services.TemplateRouter
{
	public interface ITemplateRouter
	{
		public bool TryRoute(HttpContext context, out RouteMatch? match);
	}
}
