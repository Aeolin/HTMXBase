using HandlebarsDotNet;

namespace MongoDBSemesterProjekt.Utils
{
	public static class HandlebarsEx
	{
		public static IHandlebars Create(Action<HandlebarsConfiguration> configure = null)
		{
			var cfg = new HandlebarsConfiguration();
			configure?.Invoke(cfg);
			return Handlebars.Create(cfg);
		}
	}
}
