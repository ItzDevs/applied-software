using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.DTOs;

public class TeamDto
{
    /// <summary>
    /// The Team Id.
    /// </summary>
    public long TeamId { get; set; } // SERIAL.
    
    /// <summary>
    /// The name of the team.
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// The optional description for the team.
    /// </summary>
    public string? Description { get; set; } = null!;
    
    /// <summary>
    /// The team's default allowed permissions.
    /// </summary>
    public PackageActionPermission DefaultAllowedPermissions { get; set; } = PackageActionPermission.None;
    
    /// <summary>
    /// The team's default disallowed permissions.
    /// </summary>
    public PackageActionPermission DefaultDisallowedPermissions { get; set; } = PackageActionPermission.None;
    
    /// <summary>
    /// Created Timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated Timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// All users belonging to this team, without their user groups.
    /// </summary>
    public virtual ICollection<UserDto> Users { get; set; } = null!;
    
    /// <summary>
    /// All user groups belonging to this team, without their users.
    /// </summary>
    public virtual ICollection<UserGroupDto> UserGroups { get; set; } = new List<UserGroupDto>();
}