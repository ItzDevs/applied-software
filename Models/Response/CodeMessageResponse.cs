using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppliedSoftware.Models.Response;

/// <summary>
/// A Status Code/Message struct with optional Code and Message.
/// </summary>
public readonly struct CodeMessageResponse
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="code">Optional Status Code</param>
    /// <param name="messages">Optional Status Message.</param>
    public CodeMessageResponse(int? code = null, IEnumerable<string>? messages = null)
    {
        Code = code;
        Messages = messages;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="code">Optional Status Code</param>
    /// <param name="messages">Optional Status Message.</param>
    public CodeMessageResponse(eErrorCode? code = null, IEnumerable<string>? messages = null)
    {
        Code = (int?)code;
        Messages = messages;
    }

    /// <summary>
    /// Status Code (nullable) integer.
    /// </summary>
    [JsonPropertyName("statusCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Code { get; }

    /// <summary>
    /// Status Message (nullable) string.
    /// </summary>
    [JsonPropertyName("statusMessage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IEnumerable<string>? Messages { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, Settings.DefaultSerializerOptions);
    }

    public static CodeMessageResponse Unauthorised 
        => new(eErrorCode.Unauthorised, new[] 
            { "This action is unauthorized."});

    public static CodeMessageResponse ForbiddenAction 
        => new(eErrorCode.Forbidden, new[] 
            { "You do not have the required permissions to perform this action." });
    
    public static CodeMessageResponse ForbiddenAccess
        => new(eErrorCode.Forbidden, new[]
            {"You do not have the required permissions to access this resource."});
}
