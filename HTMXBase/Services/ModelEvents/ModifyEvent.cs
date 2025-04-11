namespace HTMXBase.Services.ModelEvents
{
	public class ModifyEvent<T>
	{
		public ModifyMode Mode { get; }
		public T Item { get; }

		public ModifyEvent(ModifyMode mode, T model)
		{
			Mode = mode;
			Item = model;
		}
	}
}
