namespace AppliedSoftware.Models.Response.Auth;

public readonly struct AuthenticationResult(
    
    string? uid = null)
{
    public string? Uid { get; } = uid;
    
}