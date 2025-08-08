using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutoDocOps.Infrastructure.Authentication;

public class OrganizationAuthorizationHandler : AuthorizationHandler<OrganizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        OrganizationRequirement requirement)
    {
        var userOrganizationId = context.User.FindFirst("OrganizationId")?.Value;
        
        if (string.IsNullOrEmpty(userOrganizationId))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // If requirement has a specific organization ID, validate it
        if (requirement.OrganizationId.HasValue)
        {
            if (Guid.TryParse(userOrganizationId, out var orgId) && 
                orgId == requirement.OrganizationId.Value)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        else
        {
            // User just needs to belong to any organization
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class OrganizationRequirement : IAuthorizationRequirement
{
    public Guid? OrganizationId { get; }

    public OrganizationRequirement(Guid? organizationId = null)
    {
        OrganizationId = organizationId;
    }
}
