
using Microsoft.Extensions.Options;

namespace HTMXBase.Services.FileStorage
{
	public class FlatFileStorage : IFileStorage
	{
		private readonly IOptions<FlatFileStorageConfig> _config;

		public FlatFileStorage(IOptions<FlatFileStorageConfig> config)
		{
			_config=config;
			Directory.CreateDirectory(_config.Value.StoragePath);
		}

		private string MakePath(string id) => Path.Combine(_config.Value.StoragePath, $"{id}.bin");

		public Task<bool> DeleteFileAsync(string id)
		{
			var path = MakePath(id);
			var exists = File.Exists(path);
			if (exists)
				File.Delete(path);

			return Task.FromResult(exists);
		}

		public Task<Stream> GetBlobAsync(string id)
		{
			var file = File.OpenRead(MakePath(id));
			return Task.FromResult<Stream>(file);
		}

		public async Task<string> StoreFileAsync(IFormFile formFile)
		{
			var id = Guid.NewGuid();
			var path = MakePath(id.ToString());
			using var file = File.Create(path);
			using var source = formFile.OpenReadStream();
			await source.CopyToAsync(file);
			await file.FlushAsync();
			return id.ToString();
		}
	}
}
