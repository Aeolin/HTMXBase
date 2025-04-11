using Microsoft.AspNetCore.Authorization;
using HTMXBase.Utils;

namespace HTMXBase.Authorization
{
	public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAttribute>
	{
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionAttribute requirement)
		{
			if (context.User.HasPermission(requirement.Permission))
				context.Succeed(requirement);

			context.PendingRequirements.OfType<PermissionAttribute>()
				.Where(x => x.IsOptional)
				.ForEach(context.Succeed);

			return Task.CompletedTask;
		}
	}
}
