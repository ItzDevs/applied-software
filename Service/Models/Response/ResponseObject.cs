using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppliedSoftware.Models.Response;

public class ResponseObject<T>
{
    public ResponseObject(int? responseCode, T? body = default, CodeMessageResponse? error = null)
    {

        if (body is null && error is null)
            throw new ArgumentException("A body or error must be declared.");
        
        ResponseCode = responseCode;
        Body = body;
        Error = error;
    }

    /// <summary>
    /// 0 = Success
    /// 1 = Failure
    /// </summary>
    [JsonPropertyName("responseCode")]
    public int? ResponseCode { get; }
    
    /// <summary>
    /// The body of the response (when successful)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("body")]
    public T? Body { get; }
    
    /// <summary>
    /// The error response when unsuccessful.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("errorResponse")]
    public CodeMessageResponse? Error { get; }

    public override string ToString()
        => JsonSerializer.Serialize(this, Settings.DefaultSerializerOptions);
}