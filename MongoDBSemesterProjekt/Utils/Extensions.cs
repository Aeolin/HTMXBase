using HandlebarsDotNet;

namespace MongoDBSemesterProjekt.Utils
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			foreach (var item in items)
				collection.Add(item);
		}
	}
}
