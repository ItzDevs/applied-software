using AppliedSoftware.Models.Request.Teams;
using AppliedSoftware.Workers;
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
public class ManagementController(
    IRepository repository) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return BadRequest("Invalid route.");
    }

    
    
    
}