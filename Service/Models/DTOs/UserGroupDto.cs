using System.Text.Json.Serialization;
using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.DTOs;

/// <summary>
/// 
/// </summary>
public class UserGroupDto
{
    /// <summary>
    /// User Group Id.
    /// </summary>
    public long UserGroupId { get; set; } // SERIAL.
    
    /// <summary>
    /// The Team Id.
    /// </summary>
    public long TeamId { get; set; } // FK -> TeamDto.Tid.
    
    /// <summary>
    /// The name of the user group.
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// An optional description for the user group.
    /// </summary>
    public string? Description { get; set; } = null;
    
    /// <summary>
    /// The allowed permissions for the user group.
    /// </summary>
    public PackageActionPermission? AllowedPermissions { get; set; }
    
    /// <summary>
    /// The explicitly denied permissions for the user group.
    /// </summary>
    public PackageActionPermission? DisallowedPermissions { get; set; }
    
    /// <summary>
    /// Whether the user group is active.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    
    /// <summary>
    /// The created timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The updated timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The Team navigation property
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public virtual TeamDto Team { get; set; } = null!;
    
    /// <summary>
    ///  The users in the user group.
    /// </summary>
    public virtual ICollection<UserDto> Users { get; set; } = new List<UserDto>();
}