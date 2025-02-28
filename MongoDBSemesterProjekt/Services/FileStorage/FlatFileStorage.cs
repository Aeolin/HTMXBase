
using Microsoft.Extensions.Options;

namespace MongoDBSemesterProjekt.Services.FileStorage
{
	public class FlatFileStorage : IFileStorage
	{
		private readonly IOptions<FlatFileStorageConfig> _config;

		public FlatFileStorage(IOptions<FlatFileStorageConfig> config)
		{
			_config=config;
		}

		public Task<bool> DeleteFileAsync(string path)
		{
			var exists = File.Exists(path);
			if (exists)
				File.Delete(path);

			return Task.FromResult(exists);
		}

		public Task<Stream> GetBlobAsync(string path)
		{
			var file = File.OpenRead(path);
			return Task.FromResult<Stream>(file);
		}

		public async Task<string> StoreFileAsync(IFormFile formFile)
		{
			var id = Guid.NewGuid();
			var path = Path.Combine(_config.Value.StoragePath, $"{id.ToString()}.bin");
			var file = File.Create(path);
			var source = formFile.OpenReadStream();
			await source.CopyToAsync(file);
			return path;
		}
	}
}
