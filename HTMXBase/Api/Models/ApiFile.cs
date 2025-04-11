namespace HTMXBase.Api.Models
{
	public class ApiFile
	{
		public required string Name { get; set; }
		public ApiFileType Type { get; set; }
		public string? MimeType { get; set; }
	}
}
