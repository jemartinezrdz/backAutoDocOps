namespace AutoDocOps.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Plan Plan { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    
    public Subscription()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
    
    public Subscription(Guid organizationId, Plan plan, DateTime expiresAt) : this()
    {
        OrganizationId = organizationId;
        Plan = plan;
        ExpiresAt = expiresAt;
    }
}

