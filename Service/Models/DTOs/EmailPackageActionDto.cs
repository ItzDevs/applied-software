using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace AppliedSoftware.Models.DTOs;

public class EmailPackageActionDto
{
    /// <summary>
    /// The Primary key of the email.
    /// </summary>
    public long EmailId { get; set; }
    
    /// <summary>
    /// The Package Action Id, this is also a Foreign Key to the PackageActionDto.
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
    
    /// <summary>
    /// The body of the email.
    /// </summary>
    public string? Body { get; set; } = null!;

    /// <summary>
    /// Allows fulltext searching to be performed on the body of the email.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public NpgsqlTsVector EmailTsVector { get; set; } = null!;

    /// <summary>
    /// 
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string UploadedById { get; set; } = null!;
    
    /// <summary>
    /// The created timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The updated timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The navigation property to the package action.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]

    public PackageActionDto PackageAction { get; set; } = null!;
    
    /// <summary>
    /// The navigation property for who uploaded the email.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UserDto UploadedBy { get; set; } = null!;
    
    /// <summary>
    /// The navigation property for the attachments.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<EmailAttachmentDto> Attachments { get; set; } = new List<EmailAttachmentDto>();
}