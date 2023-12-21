namespace AppliedSoftware.Exceptions;

public class MinimumPermissionsNotGrantedException : Exception
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public MinimumPermissionsNotGrantedException()
    {
    }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public MinimumPermissionsNotGrantedException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="innerException"></param>
    public MinimumPermissionsNotGrantedException(Exception? innerException) : base(string.Empty, innerException)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public MinimumPermissionsNotGrantedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}