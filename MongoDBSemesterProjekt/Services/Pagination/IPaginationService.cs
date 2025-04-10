using MongoDB.Driver;
using MongoDBSemesterProjekt.ApiModels;

namespace MongoDBSemesterProjekt.Services.Pagination
{
	public interface IPaginationService<T>
	{
		public Task<CursorResult<T, string?>> PaginateAsync(PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null);
		public Task<CursorResult<T, string?>> PaginateAsync(string collectionName, PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null);
		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(string collectionName, PaginationValues values, Func<T, TRes> mapper, ICollection<FilterDefinition<T>>? filterList = null);
	}
}
