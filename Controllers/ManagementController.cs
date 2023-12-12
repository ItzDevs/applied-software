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

    /// <summary>
    /// Gets all packages that a user has authorised access to.
    /// </summary>
    /// <returns></returns>
    [HttpPost("teams")]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeam newTeam)
        => (await repository.CreateTeam(newTeam)).ToResponse();

    [HttpGet("teams")]
    public async Task<IActionResult> GetTeams() 
        => (await repository.GetTeams()).ToResponse();
    
    [HttpGet("teams/{teamIdentifier}")]
    public async Task<IActionResult> GetTeam(string teamIdentifier) 
        => (await repository.GetTeam(teamIdentifier)).ToResponse();
    
    [HttpPut("teams/{teamIdentifier}")]
    public async Task<IActionResult> UpdateTeam(string teamIdentifier, [FromBody] CreateTeam updateTeam)
        => (await repository.UpdateTeam(teamIdentifier, updateTeam)).ToResponse();
    
    [HttpDelete("teams/{teamIdentifier}")]
    public async Task<IActionResult> DeleteTeam(string teamIdentifier)
        => (await repository.DeleteTeam(teamIdentifier)).ToResponse();
    
    [HttpPost("usergroups")]
    public async Task<IActionResult> CreateUserGroup([FromBody] CreateUserGroup newUserGroup)
        => (await repository.CreateUserGroup(newUserGroup)).ToResponse();
    
    [HttpGet("usergroups")]
    public async Task<IActionResult> GetUserGroups() 
        => (await repository.GetUserGroups()).ToResponse();
    
    [HttpGet("usergroups/{userGroupIdentifier}")]
    public async Task<IActionResult> GetUserGroup(string userGroupIdentifier) 
        => (await repository.GetUserGroup(userGroupIdentifier)).ToResponse();
    
    [HttpPut("usergroups/{userGroupIdentifier}")]
    public async Task<IActionResult> UpdateUserGroup(string userGroupIdentifier, [FromBody] CreateUserGroup updateUserGroup)
        => (await repository.UpdateUserGroup(userGroupIdentifier, updateUserGroup)).ToResponse();
}