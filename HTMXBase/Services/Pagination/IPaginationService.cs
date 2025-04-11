using MongoDB.Driver;
using HTMXBase.ApiModels;
using HTMXBase.Utils;

namespace HTMXBase.Services.Pagination
{
	public interface IPaginationService<T>
	{
		public Task<CursorResult<T, string?>> PaginateAsync(PaginationValues values, params FilterDefinition<T>?[] filters) => PaginateAsync(values, filters.NotNull().ToList());
		public Task<CursorResult<T, string?>> PaginateAsync(PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null);
		public Task<CursorResult<T, string?>> PaginateAsync(string collectionName, PaginationValues values, ICollection<FilterDefinition<T>>? filterList = null);

		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(PaginationValues values, Func<T, TRes> mapper, params FilterDefinition<T>?[] filters) => PaginateAsync(values, mapper, filters.NotNull().ToList());
		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(PaginationValues values, Func<T, TRes> mapper, ICollection<FilterDefinition<T>>? filterList = null) => PaginateAsync(values, x => x.Select(mapper).ToAsyncEnumerable(), filterList);
		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(PaginationValues values, Func<IEnumerable<T>, IAsyncEnumerable<TRes>> mapper, params FilterDefinition<T>?[] filters) => PaginateAsync(values, mapper, filters.NotNull().ToList());
		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(PaginationValues values, Func<IEnumerable<T>, IAsyncEnumerable<TRes>> mapper, ICollection<FilterDefinition<T>>? filterList = null);

		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(string collectionName, PaginationValues values, Func<T, TRes> mapper, params FilterDefinition<T>?[] filters) => PaginateAsync(collectionName, values, mapper, filters.NotNull().ToList());
		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(string collectionName, PaginationValues values, Func<T, TRes> mapper, ICollection<FilterDefinition<T>>? filterList = null) => PaginateAsync(collectionName, values, x => x.Select(mapper).ToAsyncEnumerable(), filterList);
		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(string collectionName, PaginationValues values, Func<IEnumerable<T>, IAsyncEnumerable<TRes>> mapper, params FilterDefinition<T>?[] filters) => PaginateAsync(collectionName, values, mapper, filters.NotNull().ToList());
		public Task<CursorResult<TRes, string?>> PaginateAsync<TRes>(string collectionName, PaginationValues values, Func<IEnumerable<T>, IAsyncEnumerable<TRes>> mapper, ICollection<FilterDefinition<T>>? filterList = null);


	}
}
