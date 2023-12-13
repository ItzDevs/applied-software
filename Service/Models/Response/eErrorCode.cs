namespace AppliedSoftware.Models.Response;

public enum eErrorCode
{
    /// <summary>
    /// There is no specific error code for the issue.
    /// </summary>
    None = 0,

    /// <summary>
    /// No results in the search page.
    /// </summary>
    EmptyResults = 95,
    
    /// <summary>
    /// There were no changes to apply.
    /// </summary>
    NothingToUpdate = 100,

    /// <summary>
    /// If the page size is less than 1, or greater than 100
    /// </summary>
    InvalidPageSize = 102,
    
    /// <summary>
    /// The provided filter was invalid.
    /// </summary>
    InvalidFilter = 103,

    /// <summary>
    /// A conflict in data.
    /// </summary>
    Conflict = 301,
    
    /// <summary>
    /// An error occurred whilst validating.
    /// </summary>
    ValidationError = 399,
    
    /// <summary>
    /// Bad Request.
    /// </summary>
    BadRequest = 400,
    
    /// <summary>
    /// The unauthorised error code.
    /// </summary>
    Unauthorised = 401,
    
    /// <summary>
    /// Forbidden to access the requested resource.
    /// </summary>
    Forbidden = 403,
    
    /// <summary>
    /// Not Found.
    /// </summary>
    NotFound = 404,
    
    /// <summary>
    /// The cache service being unavailable
    /// </summary>
    CacheServiceUnavailable = 410,

    /// <summary>
    /// The cache entry wasn't found, most likely expired.
    /// </summary>
    CacheEntryNotFound = 411,

    /// <summary>
    /// A generic error occurred.
    /// </summary>
    ServiceUnavailable = 430,
    
    
}