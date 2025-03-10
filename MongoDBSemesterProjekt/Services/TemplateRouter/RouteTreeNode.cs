using MongoDBSemesterProjekt.Database.Models;

namespace MongoDBSemesterProjekt.Services.TemplateRouter
{
	public class RouteTreeNode
	{
		public string? Path { get; init; }
		public List<RouteTreeNode> Children { get; init; } = new();
		public RouteTemplateModel? RouteTemplate { get; set; }
		public string? RouteParameterName { get; init; }

		public bool Matches(ReadOnlySpan<char> path, Dictionary<string, string> routeValues)
		{
			if (Path == null) // wildcard
			{
				if(RouteParameterName != null)
					routeValues[RouteParameterName] = path.ToString();
				
				return true;
			}

			return path.Equals(Path.AsSpan(), StringComparison.OrdinalIgnoreCase);
		}

		public static RouteTreeNode Wildcard(string parameterName, RouteTemplateModel? routeTemplate = null)
		{
			return new RouteTreeNode(null, parameterName, routeTemplate);
		}

		public static RouteTreeNode Create(string path, RouteTemplateModel? routeTemplate = null)
		{
			return new RouteTreeNode(path, null, routeTemplate);
		}

		public RouteTreeNode(string? path, string? routeParameterName, RouteTemplateModel? routeTemplate = null)
		{
			Path = path;
			RouteParameterName = routeParameterName;
			RouteTemplate = routeTemplate;
		}
	}
}
