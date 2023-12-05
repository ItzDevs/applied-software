namespace AppliedSoftware.Models.Response.Auth;

public readonly struct AuthenticationResult(
    bool isValid, 
    string? uid = null)
{

    public bool IsValid { get; } = isValid;

    public string? Uid { get; } = uid;
    
    public static readonly AuthenticationResult Invalid = new (false);
}