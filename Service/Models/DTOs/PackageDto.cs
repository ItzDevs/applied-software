using System.Text.Json.Serialization;

namespace AppliedSoftware.Models.DTOs;

/// <summary>
/// The package.
/// </summary>
public class PackageDto
{
    /// <summary>
    /// The Package Id.
    /// </summary>
    public long PackageId { get; set; } // SERIAL.

    /// <summary>
    /// The name of the package.
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// The description for the package.
    /// </summary>
    public string? Description { get; set; } = null!;
    
    /// <summary>
    /// Created Timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated Timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The administrators of the package - they will ignore all other permissions.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<UserDto> Administrators { get; set; } = new List<UserDto>();
    
    /// <summary>
    /// The teams with access to the package.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<TeamDto> Teams { get; set; } = new List<TeamDto>();
    
    /// <summary>
    /// All the actions that are available for the package.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual ICollection<PackageActionDto> Actions { get; set; } = new List<PackageActionDto>();
}