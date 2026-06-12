using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Billing;

public class Plan : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }

    /// <summary>-1 means unlimited.</summary>
    public int WorkspaceLimit { get; set; }

    /// <summary>-1 means unlimited.</summary>
    public int TestimonialLimit { get; set; }

    /// <summary>-1 means unlimited.</summary>
    public int WidgetLimit { get; set; }

    public bool HasVideoTestimonials { get; set; }
    public bool HasAiFeatures { get; set; }
    public bool HasImportSources { get; set; }
    public bool HasFullAnalytics { get; set; }
    public bool HasWhiteLabel { get; set; }
    public bool HasCustomDomain { get; set; }
    public bool HasTeamMembers { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasWebhooks { get; set; }
    public bool HasCustomEmailSender { get; set; }
    public bool HasAdvancedAiInsights { get; set; }
    public bool HasPrioritySupport { get; set; }
}

public static class PlanCodes
{
    public const string Starter = "starter";
    public const string Pro = "pro";
    public const string Agency = "agency";
    public const string AgencyPlus = "agency-plus";
}
