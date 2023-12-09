using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.DTOs;

public class GlobalPermissionDto
{
    /// <summary>
    /// The Global Permission Id.
    /// </summary>
    public long GlobalPermissionId { get; set; }
    
    /// <summary>
    /// The user id.
    /// </summary>
    public string UserId { get; set; }
    
    /// <summary>
    /// The global permission.
    /// </summary>
    public GlobalPermission GrantedGlobalPermission { get; set; }
    
    /// <summary>
    /// Created Timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated Timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The navigation property for the user. 
    /// </summary>
    public virtual UserDto User { get; set; } = null!;
}