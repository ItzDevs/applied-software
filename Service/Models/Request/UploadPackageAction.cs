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

public class EmailAct
{
    /// <summary>
    ///  Only required when using <see cref="AppliedSoftware.Models.Enums.ActAction.Upload"/>
    /// </summary>
    public byte[]? File { get; set; }
    
    /// <summary>
    ///  Only required when using <see cref="AppliedSoftware.Models.Enums.ActAction.ViewEmail"/>
    /// </summary>
    public string? SearchEmailContent { get; set; }
    
    /// <summary>
    /// Only required when using <see cref="AppliedSoftware.Models.Enums.ActAction.AppendAttachment" />
    /// </summary>
    public byte[][]? AttachmentBytes { get; set; }
    
    /// <summary>
    /// Only required when using <see cref="AppliedSoftware.Models.Enums.ActAction.Remove" /> or <see cref="AppliedSoftware.Models.Enums.ActAction.AppendAttachment" />
    /// </summary>
    public string? EmailIdentifier { get; set; }
}