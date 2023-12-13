using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace AppliedSoftware.Models.DTOs;

public class EmailPackageActionDto
{
    /// <summary>
    /// The Primary Key, this is also a Foreign Key to the PackageActionDto.
    /// </summary>
    public long PackageActionId { get; set; } // PK,FK -> PackageActions.PackageActionId.
    
    /// <summary>
    /// The recipients email addresses, for multiple emails these are separated by a comma.
    /// </summary>
    public string? Recipients { get; set; }
    
    /// <summary>
    /// The sender of the email.
    /// </summary>
    public string? Sender { get; set; }

    /// <summary>
    /// The subject of the email.
    /// </summary>
    public string? Subject { get; set; }
    
    public string? Body { get; set; } = null!;

    /// <summary>
    /// Allows fulltext searching to be performed on the body of the email.
    /// </summary>
    public NpgsqlTsVector EmailTsVector { get; set; } = null!;
    
    /// <summary>
    /// The navigation property for the attachments.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<EmailAttachmentDto> Attachments { get; set; } = new List<EmailAttachmentDto>();
}