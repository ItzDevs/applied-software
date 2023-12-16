using System.Security.Claims;
using FirebaseAdmin.Auth;

namespace AppliedSoftware.Workers;

/// <summary>
/// A singleton worker that is used to run authorization checks on users.
/// </summary>
public class Authentication(
    ILogger<Authentication> logger) : IAuthentication
{
    private FirebaseAuth? _auth;
    
    public async Task StartAsync(
        CancellationToken ct = default)
    {   

        _auth = FirebaseAuth.DefaultInstance;
        
        logger.LogInformation("Authentication worker started");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="Exception">Throws </exception>
    public string? ExtractUserId(ClaimsPrincipal? user)
        => user?.FindFirst("user_id")?.Value;
}