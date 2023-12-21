namespace AppliedSoftware.Exceptions;

public class UnauthorisedException : Exception
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public UnauthorisedException()
    {
    }
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public UnauthorisedException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="innerException"></param>
    public UnauthorisedException(Exception? innerException) : base(string.Empty, innerException)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public UnauthorisedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}