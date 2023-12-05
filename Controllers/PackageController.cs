using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Controllers;

[Asp.Versioning.ApiVersion(1)]
[ApiController]
[Route("api/v{version:apiVersion}/packages")]
[Authorize]
public class PackageController : ControllerBase
{
    
    /// <summary>
    /// Gets all packages that a user has authorised access to.
    /// </summary>
    /// <param name="userToken"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetPackages()
    {
        return Ok();
    }
}