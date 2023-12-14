using System.Text.Json.Serialization;
using AppliedSoftware.Models.DTOs;

namespace AppliedSoftware.Models.Response.PackageActionsAct;

public class ActionResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<EmailPackageActionDto>? Emails { get; set; }
}