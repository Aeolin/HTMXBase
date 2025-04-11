using MongoDB.Driver;
using System.Collections;
using System.Reflection.Metadata;

namespace HTMXBase.Database.InterceptingShim
{
	public class InterceptionEventsCollection<TDocument> : ICollection<IInterceptionEvents<TDocument>>, IInterceptionEvents<TDocument>
	{
		private List<IInterceptionEvents<TDocument>> _interceptors = new List<IInterceptionEvents<TDocument>>();

		public int Count => _interceptors.Count;
		public bool IsReadOnly => false;

		public void Add(IInterceptionEvents<TDocument> item) => _interceptors.Add(item);
		public void Clear() => _interceptors.Clear();
		public bool Contains(IInterceptionEvents<TDocument> item) => _interceptors.Contains(item);
		public void CopyTo(IInterceptionEvents<TDocument>[] array, int arrayIndex) => _interceptors.CopyTo(array, arrayIndex);
		public IEnumerator<IInterceptionEvents<TDocument>> GetEnumerator() => _interceptors.GetEnumerator();
		public bool Remove(IInterceptionEvents<TDocument> item) => _interceptors.Remove(item);
		IEnumerator IEnumerable.GetEnumerator() => _interceptors.GetEnumerator();

		public TDocument OnInsert(TDocument document)
		{
			TDocument result = document;
			foreach (var interceptor in _interceptors)
				result = interceptor.OnInsert(result);

			return result;
		}

		public TDocument OnReplace(TDocument document)
		{
			TDocument result = document;
			foreach (var interceptor in _interceptors)
				result = interceptor.OnReplace(result);

			return result;
		}

		public UpdateDefinition<TDocument> OnUpdate(UpdateDefinition<TDocument> update)
		{
			UpdateDefinition<TDocument> result = update;
			foreach (var interceptor in _interceptors)
				result = interceptor.OnUpdate(result);

			return result;
		}


	}
}
