using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Controllers;

/// <summary>
/// Handles the Teams, permissions, and permission overrides.
/// </summary>
[Asp.Versioning.ApiVersion(1)]
[ApiController]
[Route("api/v{version:apiVersion}/management")]
[Authorize]
public class UserTeamController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return BadRequest("Invalid route.");
    }
    
    
    
    /// <summary>
    /// Gets all packages that a user has authorised access to.
    /// </summary>
    /// <returns></returns>
    [HttpPost("team")]
    public async Task<IActionResult> CreateTeam()
    {
        
        return Ok();
    }
}