using System.Text.Json.Serialization;

namespace AppliedSoftware.Models.DTOs;

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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The MIME type of the attachment.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string FileType { get; set; } = null!;

    /// <summary>
    /// The path to the file.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string FilePath { get; set; } = null!;
    
    /// <summary>
    /// The navigation property to the email package action.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmailPackageActionDto EmailPackageAction { get; set; } = null!;
}