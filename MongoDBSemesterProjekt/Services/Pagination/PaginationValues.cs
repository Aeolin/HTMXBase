using Microsoft.Extensions.Primitives;
using MongoDBSemesterProjekt.Database.Models;
using MongoDBSemesterProjekt.Services.TemplateRouter;
using MongoDBSemesterProjekt.Utils;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using YamlDotNet.Core;

namespace MongoDBSemesterProjekt.Services.Pagination
{
	public struct PaginationValues
	{
		public string? CursorNext { get; init; }
		public string? CursorPrevious { get; init; }
		public int Limit { get; init; }
		public bool Ascending { get; init; }
		public IEnumerable<string> Columns { get; init; }

		public const string LIMIT_KEY = "limit";
		public const string CURSOR_PREV_KEY = "prev";
		public const string CURSOR_NEXT_KEY = "next";
		public const string ASCENDING_KEY = "asc";
		public const string COLUMNS_KEY = "orderBy";
		public static readonly FrozenSet<string> PAGINATION_VALUES = [LIMIT_KEY, CURSOR_PREV_KEY, CURSOR_NEXT_KEY, ASCENDING_KEY, COLUMNS_KEY];

		public PaginationDirection DecomposeCursor(out Dictionary<string, string>? cursorBody)
		{
			var cursor = "{}";
			var direction = PaginationDirection.Forward;
			if (string.IsNullOrEmpty(CursorPrevious) == false)
			{
				direction = PaginationDirection.Backward;
				cursor = CursorPrevious;
			}

			if (string.IsNullOrEmpty(CursorNext) == false)
			{
				direction = PaginationDirection.Forward;
				cursor = CursorNext;
			}

			unsafe
			{
				var byteLen = Encoding.UTF8.GetByteCount(cursor);
				Span<byte> buffer = stackalloc byte[Base64.GetMaxDecodedFromUtf8Length(byteLen)];
				Encoding.UTF8.GetBytes(cursor, buffer);
				if (Base64.DecodeFromUtf8InPlace(buffer, out var written) == OperationStatus.Done)
				{
					cursorBody = JsonSerializer.Deserialize<Dictionary<string, string>>(buffer.Slice(written));
				}
				else
				{
					cursorBody = null;
				}
			}

			return direction;
		}

		public PaginationValues(string? cursorNext, string? cursorPrev, int limit, bool ascending, IEnumerable<string> columns)
		{
			CursorNext = cursorNext;
			CursorPrevious = cursorPrev;
			Limit = limit;
			Ascending = ascending;
			Columns = columns;
		}

		public static PaginationValues FromRouteMatch(RouteMatch match)
		{
			var cursorNext = match.QueryValues.GetParsedValueOrDefault<string?>(CURSOR_NEXT_KEY, null);
			var cursorPrevious = match.QueryValues.GetParsedValueOrDefault<string?>(CURSOR_PREV_KEY, null);
			var limit = match.QueryValues.GetParsedValueOrDefault<int>(LIMIT_KEY, match.RouteTemplateModel.PaginationLimit);
			var ascending = match.QueryValues.GetParsedValueOrDefault<bool>(ASCENDING_KEY, true);
			var columns = match.QueryValues.GetParsedValueOrDefault<IEnumerable<string>>(COLUMNS_KEY, match.RouteTemplateModel.PaginationColumns ?? ["_id"]);
			return new PaginationValues(cursorNext, cursorPrevious, limit, ascending, columns);
		}
	}
}
