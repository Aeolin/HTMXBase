namespace MongoDBSemesterProjekt.Utils
{
	public class CursorResult<TData, TCursor>
	{
		public TCursor Cursor { get; init; }
		public TData Data { get; init; }

		public CursorResult(TCursor cursor, TData data)
		{
			Cursor=cursor;
			Data=data;
		}

	}

	public static class CursorResult
	{
		public static CursorResult<TData, TCursor> Create<TData, TCursor>(TCursor cursor, TData data) => new CursorResult<TData, TCursor>(cursor, data);
	}
}
