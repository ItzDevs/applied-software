using AppliedSoftware.Models.Request.Teams;
using AppliedSoftware.Workers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Controllers;

/// <summary>
/// Handles the User Groups, permissions, and permission overrides.
/// </summary>
[Asp.Versioning.ApiVersion(1)]
[ApiController]
[Route("api/v{version:apiVersion}/management/usergroups")]
[Authorize]
public class UserGroupsController(
    IRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUserGroup([FromBody] CreateUserGroup newUserGroup)
        => (await repository.CreateUserGroup(newUserGroup)).ToResponse();
    
    [HttpGet]
    public async Task<IActionResult> GetUserGroups() 
        => (await repository.GetUserGroups()).ToResponse();
    
    [HttpGet("{userGroupIdentifier}")]
    public async Task<IActionResult> GetUserGroup(string userGroupIdentifier) 
        => (await repository.GetUserGroup(userGroupIdentifier)).ToResponse();
    
    [HttpPut("{userGroupIdentifier}")]
    public async Task<IActionResult> UpdateUserGroup(string userGroupIdentifier, [FromBody] CreateUserGroup updateUserGroup)
        => (await repository.UpdateUserGroup(userGroupIdentifier, updateUserGroup)).ToResponse();
    
    [HttpDelete("{userGroupIdentifier}")]
    public async Task<IActionResult> DeleteUserGroup(string userGroupIdentifier)
        => (await repository.DeleteUserGroup(userGroupIdentifier)).ToResponse();
    
    [HttpGet("{userGroupIdentifier}/users")]
    public async Task<IActionResult> GetUsersInUserGroup(string userGroupIdentifier) 
        => (await repository.GetUsersInUserGroup(userGroupIdentifier)).ToResponse();
    
    [HttpPut("{userGroupIdentifier}/users/add")]
    public async Task<IActionResult> AddUsersToUserGroup(string userGroupIdentifier, string? userIds)
        => (await repository.AddUsersToUserGroup(userGroupIdentifier, userIds)).ToResponse();
    
    [HttpPut("{userGroupIdentifier}/users/remove")]
    public async Task<IActionResult> RemoveUsersFromUserGroup(string userGroupIdentifier, string? userIds)
        => (await repository.RemoveUsersFromUserGroup(userGroupIdentifier, userIds)).ToResponse();
    
    
}