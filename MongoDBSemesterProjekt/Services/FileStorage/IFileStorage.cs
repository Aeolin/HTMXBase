namespace MongoDBSemesterProjekt.Services.FileStorage
{
	public interface IFileStorage
	{
		public Task<string> StoreFileAsync(IFormFile file);
		public Task<bool> DeleteFileAsync(string path);
		public Task<Stream> GetBlobAsync(string path);
	}
}
