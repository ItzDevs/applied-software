namespace AppliedSoftware.Models.Request;

public abstract class EmailAct
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
    /// Only required when using <see cref="AppliedSoftware.Models.Enums.ActAction.AddAttachments" />
    /// </summary>
    public EmailAttachment[]? Attachments { get; set; }
    
    /// <summary>
    /// Only required when using <see cref="AppliedSoftware.Models.Enums.ActAction.Remove" /> or <see cref="AppliedSoftware.Models.Enums.ActAction.AppendAttachment" />
    /// </summary>
    public long? EmailId { get; set; }
}