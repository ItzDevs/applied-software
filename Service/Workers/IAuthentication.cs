using System.Security.Claims;

namespace AppliedSoftware.Workers;

public interface IAuthentication : IWorkerService
{
    /// <summary>
    /// Extracts the user ID from a <see cref="ClaimsPrincipal"/>.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    string? ExtractUserId(ClaimsPrincipal? user);
}