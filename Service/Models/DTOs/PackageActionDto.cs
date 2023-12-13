using System.Text.Json.Serialization;
using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.DTOs;

/// <summary>
/// The package action.
/// </summary>
public class PackageActionDto
{
    /// <summary>
    /// The Package Action Id.
    /// </summary>
    public long PackageActionId { get; set; } // SERIAL.
    
    /// <summary>
    /// The package Id.
    /// </summary>
    public long PackageId { get; set; } // FK -> Packages.PackageId.
    
    /// <summary>
    /// The type of package action - this can be used to determine how to deserialize the action.
    /// </summary>
    public PackageActionType PackageActionType { get; set; } = PackageActionType.None;
    
    /// <summary>
    /// The package navigation object.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual PackageDto Package { get; set; } = null!;
    
    /// <summary>
    /// The overrides for the package action.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<UserPermissionOverrideDto> UserPermissionOverrides { get; set; } = new List<UserPermissionOverrideDto>();
    
    /// <summary>
    /// The overrides for the package action.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<UserGroupPermissionOverrideDto> TeamPermissionOverrides { get; set; } = new List<UserGroupPermissionOverrideDto>();
}