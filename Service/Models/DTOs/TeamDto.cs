using System.Text.Json.Serialization;
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
    public PackageUserPermission DefaultAllowedPermissions { get; set; } = PackageUserPermission.None;
    
    /// <summary>
    /// Whether the team has been deleted.
    /// </summary>
    public bool Deleted { get; set; } = false;
    
    /// <summary>
    /// The package that the team belongs to.
    /// </summary>
    public long? PackageId { get; set; }
    
    /// <summary>
    /// Created Timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated Timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The navigation property to the team.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual PackageDto Package { get; set; } = null!;
    
    /// <summary>
    /// All users belonging to this team, without their user groups.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<UserDto> Users { get; set; } = null!;
    
    /// <summary>
    /// All user groups belonging to this team, without their users.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<UserGroupDto> UserGroups { get; set; } = new List<UserGroupDto>();
}