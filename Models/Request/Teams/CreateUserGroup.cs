using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.Request.Teams;

public class CreateUserGroup
{
    public long? TeamId { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string? Description { get; set; } = null;
    
    // Nullable as it by default can inherit permissions from the Team.
    public PackageActionPermission? AllowedPermissions { get; set; }
    
    // Nullable as it by default as permissions that are not granted by the Team 
    // will not be granted otherwise.
    public PackageActionPermission? DisallowedPermissions { get; set; }
}