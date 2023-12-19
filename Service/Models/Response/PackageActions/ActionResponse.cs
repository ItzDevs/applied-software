using System.Text.Json.Serialization;

namespace AppliedSoftware.Models.Response.PackageActions;

public class ActionResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<EmailPackageActionResponse>? Emails { get; set; }
}