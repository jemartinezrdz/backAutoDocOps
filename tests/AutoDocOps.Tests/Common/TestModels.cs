namespace AutoDocOps.Tests.Common;

// Test classes for AutoMapper testing
public class TestProject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TestProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class TestSessionData
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
}

public class TestCheckoutRequest
{
    public Guid OrganizationId { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}
