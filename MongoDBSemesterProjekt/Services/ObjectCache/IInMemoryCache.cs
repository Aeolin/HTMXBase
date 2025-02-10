namespace MongoDBSemesterProjekt.Services.ObjectCache
{
	public interface IInMemoryCache<K, V>
	{
		public bool HasKey(K key);
		public bool TryGet(K key, out V value);
		public void Set(K key, V value);
		public bool Unset(K key);
		public void Clean();
	}
}
