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
public class UserTeamController(
    IRepository repository) : ControllerBase
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
    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeam newTeam)
        => (await repository.CreateTeam(newTeam)).ToResponse();

    [HttpGet("teams")]
    public async Task<IActionResult> GetTeams() => throw new NotImplementedException();
    
    [HttpGet("teams/{teamIdentifier}")]
    public async Task<IActionResult> GetTeam(string teamIdentifier) 
        => (await repository.GetTeam(teamIdentifier)).ToResponse();
}