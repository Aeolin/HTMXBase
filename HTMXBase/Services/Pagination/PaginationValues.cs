﻿using Microsoft.Extensions.Primitives;
using HTMXBase.Database.Models;
using HTMXBase.Services.TemplateRouter;
using HTMXBase.Utils;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using YamlDotNet.Core;
using System.Text.RegularExpressions;

namespace HTMXBase.Services.Pagination
{
	public struct PaginationValues
	{
		public string? CursorNext { get; init; }
		public string? CursorPrevious { get; init; }
		public int Limit { get; init; }
		public bool Ascending { get; init; }
		public IEnumerable<string> Columns { get; init; }

		public const string LIMIT_KEY = "limit";
		public const string CURSOR_PREV_KEY = "cursorPrevious";
		public const string CURSOR_NEXT_KEY = "cursorNext";
		public const string ASCENDING_KEY = "asc";
		public const string COLUMNS_KEY = "orderBy";
		public static readonly FrozenSet<string> PAGINATION_VALUES = [LIMIT_KEY, CURSOR_PREV_KEY, CURSOR_NEXT_KEY, ASCENDING_KEY, COLUMNS_KEY];


		public PaginationValues(string? cursorNext, string? cursorPrev, int limit, bool ascending, IEnumerable<string> columns)
		{
			CursorNext = cursorNext;
			CursorPrevious = cursorPrev;
			Limit = limit;
			Ascending = ascending;
			Columns = columns;
		}


		public static PaginationValues FromRequest(HttpRequest request, bool ignoreForm = false)
		{
			if (request.HasFormContentType && ignoreForm == false)
			{
				var form = request.Form;
				var cursorNext = form.GetParsedValueOrDefault<string?>(CURSOR_NEXT_KEY, null);
				var cursorPrevious = form.GetParsedValueOrDefault<string?>(CURSOR_PREV_KEY, null);
				var limit = form.GetParsedValueOrDefault<int>(LIMIT_KEY, 20);
				var ascending = form.GetParsedValueOrDefault<bool>(ASCENDING_KEY, true);
				var columns = form.GetParsedValueOrDefault<IEnumerable<string>>(COLUMNS_KEY, new[] { "_id" });
				return new PaginationValues(cursorNext, cursorPrevious, limit, ascending, columns);
			}
			else
			{
				var cursorNext = request.Query[CURSOR_NEXT_KEY].FirstOrDefault();
				var cursorPrevious = request.Query[CURSOR_PREV_KEY].FirstOrDefault();
				var limit = request.Query.GetParsedValueOrDefault<int>(LIMIT_KEY, 20);
				var ascending = request.Query.GetParsedValueOrDefault<bool>(ASCENDING_KEY, true);
				var columns = request.Query.GetParsedValueOrDefault<IEnumerable<string>>(COLUMNS_KEY, new[] { "_id" });
				return new PaginationValues(cursorNext, cursorPrevious, limit, ascending, columns);
			}
		}

		public static PaginationValues FromRouteMatch(RouteMatch match)
		{
			var cursorNext = match.QueryValues.GetParsedValueOrDefault<string?>(CURSOR_NEXT_KEY, null);
			var cursorPrevious = match.QueryValues.GetParsedValueOrDefault<string?>(CURSOR_PREV_KEY, null);
			var limit = match.QueryValues.GetParsedValueOrDefault<int>(LIMIT_KEY, match.RouteTemplateModel.PaginationLimit);
			var ascending = match.QueryValues.GetParsedValueOrDefault<bool>(ASCENDING_KEY, true);
			var columns = match.QueryValues.GetParsedValueOrDefault<IEnumerable<string>>(COLUMNS_KEY, new[] { "_id" });
			return new PaginationValues(cursorNext, cursorPrevious, limit, ascending, columns);
		}

	}
}
