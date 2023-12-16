using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Models.Response;

/// <summary>
/// Stores the status of an action.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct StatusContainer<T>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="responseData"></param>
    /// <param name="error"></param>
    public StatusContainer
     (
        HttpStatusCode statusCode, 
        T? responseData = default, 
        CodeMessageResponse? error = null
     )
    {
        this.StatusCode = statusCode;
        this.Success = ((int)statusCode >= 200 && (int)statusCode < 300);
        ResponseData = new(Success ? 1 : 0, responseData, error);
    }

    /// <summary>
    /// The success status.
    /// </summary>
    public bool Success { get; }
    
    /// <summary> 
    /// The status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    public ResponseObject<T> ResponseData { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, Settings.DefaultSerializerOptions);
    }

    /// <summary>
    /// A single method to map responses to the user.
    /// 
    /// <para>
    ///     This method is designed to be used on RESTful endpoints.
    /// </para>
    /// </summary>
    /// <returns></returns>
    public ObjectResult ToResponse()
        => new (ResponseData)
        {
            StatusCode = (int)StatusCode
        };

}