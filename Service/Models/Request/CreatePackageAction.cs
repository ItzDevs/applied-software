using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.Request;

public class CreatePackageAction
{
    public PackageActionType PackageActionType { get; set; } = PackageActionType.None;
    
    public IEnumerable<CreatePermissionOverride>? PermissionOverrides { get; set; }
}