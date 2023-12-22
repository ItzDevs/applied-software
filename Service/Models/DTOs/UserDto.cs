using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FirebaseAdmin.Auth;

namespace AppliedSoftware.Models.DTOs;

public class UserDto
{
    public UserDto()
    {
        
    }

    internal UserDto(UserRecord userRecord)
    {
        Uid = userRecord.Uid;
        DisplayName = userRecord.DisplayName ?? userRecord.Email;
        FirebaseDisplayName = userRecord.DisplayName ?? userRecord.Email;
        Disabled = userRecord.Disabled;
        FirebaseDisabled = userRecord.Disabled;
        CreatedAtUtc = userRecord.UserMetaData.CreationTimestamp ?? DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Gets the user ID of this user.
    /// </summary>
    public string Uid { get; set; } = null!;

    /// <summary>
    /// Gets the user's display name, if available. Otherwise null.
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// The firebase display name; this is used to track changes to the display name upstream.
    /// </summary>
    public string? FirebaseDisplayName { get; set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the user account is disabled or not.
    /// </summary>
    public bool Disabled { get; set; } = true;
    
    public bool FirebaseDisabled { get; set; } = true;

    /// <summary>
    /// If the user is deleted (soft-deleted).
    /// </summary>
    public bool Deleted { get; set; } = false;
    
    /// <summary>
    /// The creation date of the user (when synced from Firebase).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
    
    /// <summary>
    /// The updated timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }
    
    /// <summary>
    /// The navigation property for the user's global permission.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual GlobalPermissionDto GlobalPermission { get; set; } = null!;
    
    /// <summary>
    /// The navigation property for the packages the user is an administrator of.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<PackageDto> PackageAdministrator { get; set; } = new List<PackageDto>();
    
    /// <summary>
    /// The navigation property for the packages the user is a member of.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<TeamDto> Teams { get; set; } = new List<TeamDto>();
    
    /// <summary>
    /// The navigation property for the packages the users permission "user groups".
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<UserGroupDto> UserGroups { get; set; } = new List<UserGroupDto>();
    
    /// <summary>
    /// The navigation property for the packages the users permission overrides (individual users).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<UserPermissionOverrideDto> UserPermissionOverrides { get; set; } = new List<UserPermissionOverrideDto>();

}