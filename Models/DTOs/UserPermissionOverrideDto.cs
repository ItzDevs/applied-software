using System.Text.Json.Serialization;
using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.DTOs;

/// <summary>
/// An individual user permissions override within a package or package action.
/// This overrides the default UserGroup permissions.
/// </summary>
public class UserPermissionOverrideDto
{
    /// <summary>
    /// The permission override id.
    /// </summary>
    public long UserPermissionOverrideId { get; set; } // SERIAL.

    /// <summary>
    /// The User the override applies to.
    /// </summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>
    /// The Package the override applies to.
    /// </summary>
    public long PackageId { get; set; } // FK -> PackageDto.Pid.
    
    /// <summary>
    /// The Package action (nullable) the override applies to.
    /// </summary>
    public long? PackageActionId { get; set; } // FK -> PackageActionDto.PackageAction
    
    /// <summary>
    /// The reason why the override is being applied; useful for auditing.
    /// </summary>
    public string? Reason { get; set; } = null!;
    
    /// <summary>
    /// Whether the override is active or not.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether the override has been deleted or not.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    
    /// <summary>
    /// The permissions the user will have.
    /// </summary>
    public PackageActionPermission AllowedPermissions { get; set; } = PackageActionPermission.None;
    
    /// <summary>
    /// The permissions the user has been explicitly denied.
    /// </summary>
    public PackageActionPermission DisallowedPermissions { get; set; } = PackageActionPermission.None;
    
    /// <summary>
    /// Created Timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated Timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public virtual UserDto User { get; set; } = null!;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public virtual PackageDto Package { get; set; } = null!;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public virtual PackageActionDto? PackageAction { get; set; } = null!;
}