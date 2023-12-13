using System.Text.Json.Serialization;
using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.DTOs;

/// <summary>
/// A User Group override
/// </summary>
public class UserGroupPermissionOverrideDto 
{
    /// <summary>
    /// The Permission Override Id.
    /// </summary>
    public long UserGroupOverrideId { get; set; } // SERIAL.
    
    /// <summary>
    /// The User Group the override applies to.
    /// </summary>
    public long UserGroupId { get; set; } // FK -> UserGroupDto.UserGroupId.
    
    /// <summary>
    /// The Package the override applies to.
    /// </summary>
    public long PackageId { get; set; } // FK -> PackageDto.Pid.

    /// <summary>
    /// The package action (nullable) the override applies to.
    /// </summary>
    public long PackageActionId { get; set; } // FK -> PackageActionDto.PackageAction
    
    /// <summary>
    /// The reason why the override is being applied; useful for auditing.
    /// </summary>
    public string? Reason { get; set; } = null!;
    
    /// <summary>
    /// The permissions the user will have.
    /// </summary>
    public PackageActionPermission AllowedPermissions { get; set; } = PackageActionPermission.None;
    
    /// <summary>
    /// The permissions the user has been explicitly denied.
    /// </summary>
    public PackageActionPermission DisallowedPermissions { get; set; } = PackageActionPermission.None;
    
    /// <summary>
    /// Whether the override is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether the override has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    
    /// <summary>
    /// Created Timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated Timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The user group this override applies to.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual UserGroupDto UserGroup { get; set; } = null!;

    /// <summary>
    /// The package this override applies to, this will be used when <see cref="PackageAction"/> is null.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual PackageDto Package { get; set; } = null!;
    
    /// <summary>
    /// The package action this override applies to, this property takes precedence over <see cref="Package"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual PackageActionDto? PackageAction { get; set; } = null!;
}