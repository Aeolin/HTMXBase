using Microsoft.Extensions.Options;
using Namotion.Reflection;

namespace MongoDBSemesterProjekt.Services.ObjectCache
{
	public class InMemoryCache<K, V> : IInMemoryCache<K, V>
	{
		private class CacheItem
		{
			public CacheItem(K key, V value, DateTime timeStamp)
			{
				Key = key;
				Value = value;
				TimeStamp = timeStamp;
			}

			public K Key { get; init; }
			public V Value { get; init; }
			public DateTime TimeStamp { get; set; }
		}

		private readonly Dictionary<K, CacheItem> _cache = new Dictionary<K, CacheItem>();
		private readonly IOptions<InMemoryCacheConfig> _config;

		public InMemoryCache(IOptions<InMemoryCacheConfig> config)
		{
			_config = config;
		}

		public void Clean()
		{
			if (_config.Value.MaxRetentionTime.HasValue)
			{
				var retention = _config.Value.MaxRetentionTime.Value;
				var now = DateTime.UtcNow;
				var toRemove = _cache.Values.Where(x => x.TimeStamp + retention < now).Select(x => x.Key).ToArray();
				foreach (var key in toRemove)
					_cache.Remove(key);
			}
		}

		public bool TryGet(K key, out V value)
		{
			var result = _cache.TryGetValue(key, out var item);
			value = result ? item.Value : default;
			if (result && _config.Value.RefreshOnRead)
				item.TimeStamp = DateTime.UtcNow;

			return result;
		}

		public void Set(K key, V value)
		{
			_cache[key] = new CacheItem(key, value, DateTime.UtcNow);
		}

		public bool Unset(K key) => _cache.Remove(key);
		public bool HasKey(K key) => _cache.ContainsKey(key);
	}
}
