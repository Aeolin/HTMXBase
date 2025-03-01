using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace MongoDBSemesterProjekt.Authorization
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class PermissionAttribute : Attribute, IAuthorizationRequirement, IAuthorizationRequirementData
	{
		public string Permission { get; init; }
		public string[] Roles { get; init; }
		public bool IsOptional { get; init; }
		public string Category { get; init; }

		public PermissionAttribute(string permission, bool isOptional, params string[] roles)
		{
			Permission=permission;
			Roles=roles;
			IsOptional=isOptional;
			var index = permission.IndexOf('/');
			if (index >= 0)
				Category = permission.Substring(0, index);
		}

		public PermissionAttribute(string permission, params string[] roles) : this(permission, false, roles)
		{
		}

		public IEnumerable<IAuthorizationRequirement> GetRequirements()
		{
			yield return this;
		}
	}
}
