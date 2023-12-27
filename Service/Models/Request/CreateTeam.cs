using AppliedSoftware.Models.DTOs;
using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.Request;

public class CreateTeam
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The description of the team.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The default allowed permissions for packages in the team.
    /// </summary>
    public PackageUserPermission? DefaultAllowedPermissions { get; set; }
    
    /// <summary>
    /// The package this team belongs to, nullable as a team may not necessarily belong to a package when being created.
    /// </summary>
    public long? BelongsToPackageId { get; set; }

    public bool HasIdenticalValues(TeamDto? right)
    {
        return Name?.Equals(right?.Name, StringComparison.OrdinalIgnoreCase) == true &&
               Description?.Equals(right.Description, StringComparison.OrdinalIgnoreCase) == true && 
               DefaultAllowedPermissions == right.DefaultAllowedPermissions;
    }
}