using Microsoft.AspNetCore.Authorization;
using MongoDBSemesterProjekt.Utils;

namespace MongoDBSemesterProjekt.Authorization
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
