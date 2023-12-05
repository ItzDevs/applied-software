using AppliedSoftware.Models.Response.Auth;
using FirebaseAdmin;
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
    {   // Config loaded from GOOGLE_APPLICATION_CREDENTIALS environment variable.
        FirebaseApp.Create();

        _auth = FirebaseAuth.DefaultInstance;
        
        logger.LogInformation("Authentication worker started");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="Exception">Throws </exception>
    public async Task<AuthenticationResult> IsUserAuthenticated(string token)
    {
        if(_auth is null)
            throw new Exception("Firebase Auth is not initialized");

        try
        {
            var decoded
                = await _auth.VerifyIdTokenAsync(
                    token, 
                    checkRevoked: true);

            return new(true, decoded.Uid);
        }
        catch (FirebaseAuthException)
        {
            return AuthenticationResult.Invalid;
        }
       
    }
}

public interface IAuthentication : IWorkerService
{
    Task<AuthenticationResult> IsUserAuthenticated(string token);
}

public interface IWorkerService
{
    Task StartAsync(CancellationToken ct = default);
}