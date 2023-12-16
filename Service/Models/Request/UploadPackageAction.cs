namespace AppliedSoftware.Models.Request;

/// <summary>
/// Execute an act on the package action.
/// </summary>
public class ActPackageAction
{
    public string Action { get; set; } = string.Empty;
    
    public EmailAct? Email { get; set; }
    
    public string? Filter { get; set; }
}