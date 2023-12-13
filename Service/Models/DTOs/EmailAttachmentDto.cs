using System.Text.Json.Serialization;

namespace AppliedSoftware.Models.DTOs;

/// <summary>
/// Represents an attachment to an email.
/// </summary>
public class EmailAttachmentDto
{
    /// <summary>
    /// The attachment id.
    /// </summary>
    public long AttachmentId { get; set; }

    /// <summary>
    /// The package action id.
    /// </summary>
    public long EmailPackageActionId { get; set; } // FK -> EmailPackageActions.EmailPackage
    
    /// <summary>
    /// The file name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The MIME type of the attachment.
    /// </summary>
    public string FileType { get; set; } = null!;

    /// <summary>
    /// The path to the file.
    /// </summary>
    public string FilePath { get; set; } = null!;
    
    /// <summary>
    /// The navigation property to the email package action.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public EmailPackageActionDto EmailPackageActionDto { get; set; } = null!;
}