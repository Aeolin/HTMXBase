﻿namespace HTMXBase.ApiModels.Requests
{
	public class ApiRegisterRequest
	{
		public required string Email { get; set; }
		public required string Password { get; set; }

		public string? Username { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? AvatarUrl { get; set; }
	}
}
