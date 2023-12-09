using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.Request.Teams;

public class CreateTeam
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    public string? Name { get; set; }
    
    
    public string? Description { get; set; }
    
    public PackageActionPermission DefaultAllowedPermissions { get; set; } = PackageActionPermission.None;
    
    public PackageActionPermission DefaultDisallowedPermissions { get; set; } = PackageActionPermission.None;
}