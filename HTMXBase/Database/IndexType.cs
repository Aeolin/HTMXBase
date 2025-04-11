namespace HTMXBase.Database
{
	public enum IndexType
	{
		Ascending,
		Descending,
		Geo2D,
		Geo2DSphere,
		[Obsolete("Deprecated in MongoDB 4.4, use Geo2DSphere instead.")]
		GeoHaystack,
		Hashed,
		Text
	}
}
