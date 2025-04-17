namespace HTMXBase.ApiModels.Requests
{
	public class ApiLoginRequest
	{
		public required string UsernameOrEmail { get; set; }
		public required string Password { get; set; }
	}
}
