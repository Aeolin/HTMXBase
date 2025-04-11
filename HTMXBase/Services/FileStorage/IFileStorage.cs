namespace HTMXBase.Services.FileStorage
{
	public interface IFileStorage
	{
		public Task<string> StoreFileAsync(IFormFile file);
		public Task<bool> DeleteFileAsync(string id);
		public Task<Stream> GetBlobAsync(string id);
	}
}
