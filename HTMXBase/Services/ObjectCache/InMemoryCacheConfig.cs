namespace HTMXBase.Services.ObjectCache
{
	public class InMemoryCacheConfig
	{
		public TimeSpan? MaxRetentionTime { get; set; }
		public bool RefreshOnRead { get; set; }
	
	}
}
