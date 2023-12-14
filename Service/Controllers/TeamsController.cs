using AppliedSoftware.Models.Request;
using AppliedSoftware.Workers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Controllers;

/// <summary>
/// Handles the Teams, permissions, and permission overrides.
/// </summary>
[Asp.Versioning.ApiVersion(1)]
[ApiController]
[Route("api/v{version:apiVersion}/management/teams")]
[Authorize]
public class TeamsController(
    IRepository repository) : ControllerBase
{
    /// <summary>
    /// Gets all packages that a user has authorised access to.
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeam newTeam)
        => (await repository.CreateTeam(newTeam)).ToResponse();

    [HttpGet]
    public async Task<IActionResult> GetTeams() 
        => (await repository.GetTeams()).ToResponse();
    
    [HttpGet("{teamIdentifier}")]
    public async Task<IActionResult> GetTeam(string teamIdentifier) 
        => (await repository.GetTeam(teamIdentifier)).ToResponse();
    
    [HttpPut("{teamIdentifier}")]
    public async Task<IActionResult> UpdateTeam(string teamIdentifier, [FromBody] CreateTeam updateTeam)
        => (await repository.UpdateTeam(teamIdentifier, updateTeam)).ToResponse();
    
    [HttpDelete("{teamIdentifier}")]
    public async Task<IActionResult> DeleteTeam(string teamIdentifier)
        => (await repository.DeleteTeam(teamIdentifier)).ToResponse();
}