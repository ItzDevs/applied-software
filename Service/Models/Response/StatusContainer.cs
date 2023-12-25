using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Models.Response;

/// <summary>
/// Internally used HttpStatus Wrapper. Contains an HttpStatusCode and Response Data or an error message and optional ErrorResponse. Minimum Requirement is StatusCode.
/// </summary>
public readonly struct StatusContainer
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="statusCode"></param>
    public StatusContainer(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
        Success = ((int)statusCode >= 200 && (int)statusCode < 300);
        ResponseData = null;
    }
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="error"></param>
    public StatusContainer(HttpStatusCode statusCode, CodeMessageResponse? error)
    {
        StatusCode = statusCode;
        Success = ((int)statusCode >= 200 && (int)statusCode < 300);
        ResponseData = new(Success ? 1 : 0, error);
    }

    public static implicit operator StatusContainer(HttpStatusCode code) 
        => new(code);

    /// <summary>
    /// Http Status Code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Response data.
    /// </summary>
    public ResponseObject<object>? ResponseData { get; }

    /// <summary>
    /// Success
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public bool Success { get; }

    public ObjectResult ToResponse()
        => new(ResponseData ?? null)
        {
            StatusCode = (int)StatusCode
        };
    
    /// <inheritdoc />
    public override string ToString()
        => JsonSerializer.Serialize(this, Settings.DefaultSerializerOptions);
}