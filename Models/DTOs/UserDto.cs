using System.ComponentModel.DataAnnotations;
using FirebaseAdmin.Auth;

namespace AppliedSoftware.Models.DTOs;

public class UserDto
{
    internal UserDto()
    {
        
    }

    internal UserDto(UserRecord userRecord)
    {
        Uid = userRecord.Uid;
        DisplayName = userRecord.DisplayName;
        FirebaseDisplayName = userRecord.DisplayName;
        Disabled = userRecord.Disabled;
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

    public string? FirebaseDisplayName { get; set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the user account is disabled or not.
    /// </summary>
    public bool Disabled { get; set; } = true;

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
    /// The navigation property for the packages the user is an administrator of.
    /// </summary>
    public virtual ICollection<PackageDto> PackageAdministrator { get; set; } = new List<PackageDto>();
    
    /// <summary>
    /// The navigation property for the packages the user is a member of.
    /// </summary>
    public virtual ICollection<TeamDto> Teams { get; set; } = new List<TeamDto>();
    
    /// <summary>
    /// The navigation property for the packages the users permission "user groups".
    /// </summary>
    public virtual ICollection<UserGroupDto> UserGroups { get; set; } = new List<UserGroupDto>();
    
    /// <summary>
    /// The navigation property for the packages the users permission overrides (individual users).
    /// </summary>
    public virtual ICollection<UserPermissionOverrideDto> UserPermissionOverrides { get; set; } = new List<UserPermissionOverrideDto>();

}