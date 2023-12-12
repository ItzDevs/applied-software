using AppliedSoftware.Models.DTOs;
using AppliedSoftware.Models.Request.Teams;
using AppliedSoftware.Models.Response;

namespace AppliedSoftware.Workers;

public interface IRepository
{
    /// <summary>
    /// Get the users global permissions, or return an error that can be returned to the user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<StatusContainer<GlobalPermissionDto>> GetGlobalPermissionsForUser(
        string userId);

    /// <summary>
    /// Gets the user if the user exists, or returns an error that can be returned to the user.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="includeDisabled"></param>
    /// <param name="includeDeleted"></param>
    /// <returns></returns>
    Task<StatusContainer<UserDto>> GetUser(
        string userId,
        bool includeDisabled = false,
        bool includeDeleted = false);

    /// <summary>
    /// Create a team if the required permissions are granted, or return an error that can be returned to the user.
    /// </summary>
    /// <param name="newTeam"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer> CreateTeam(
        CreateTeam newTeam,
        bool isInternal = false);

    /// <summary>
    /// Gets teams.
    /// </summary>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer<IEnumerable<TeamDto>>> GetTeams(
        bool isInternal = false);
    
    /// <summary>
    /// Get a team by the id or name if the required permissions are granted, or return an error that can be returned to the user.
    /// </summary>
    /// <param name="teamIdentifier"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer<TeamDto>> GetTeam(
        string teamIdentifier,
        bool isInternal = false);

    /// <summary>
    /// Update an existing team by the id or name if the required permissions are granted, or return an error that can be returned to the user.
    /// </summary>
    /// <param name="teamIdentifier"></param>
    /// <param name="updateTeam"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer<TeamDto>> UpdateTeam(
        string teamIdentifier,
        CreateTeam updateTeam,
        bool isInternal = false);

    /// <summary>
    /// Delete an existing team by the id or name if the required permissions are granted, or return an error that can be returned to the user.
    /// </summary>
    /// <param name="teamIdentifier"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer> DeleteTeam(
        string teamIdentifier,
        bool isInternal = false);

    /// <summary>
    /// Create a new user group if the required permissions are granted, or return an error that can be returned
    /// </summary>
    /// <param name="newUserGroup"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer> CreateUserGroup(
        CreateUserGroup newUserGroup,
        bool isInternal = false);

    /// <summary>
    /// Get the user groups if the required permissions are granted (or the user is a member of), or return an error that can be returned to the
    /// </summary>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer<IEnumerable<UserGroupDto>>> GetUserGroups(
        bool isInternal = false);

    /// <summary>
    /// Get a user group by the id or name if the required permissions are granted, or return an error
    /// </summary>
    /// <param name="userGroupIdentifier"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer<UserGroupDto>> GetUserGroup(
        string userGroupIdentifier,
        bool isInternal = false);

    /// <summary>
    /// Update an existing user group by the id or name if the required permissions are granted, or return an error that can be returned to the user.
    /// </summary>
    /// <param name="userGroupIdentifier"></param>
    /// <param name="updateUserGroup"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer<UserGroupDto>> UpdateUserGroup(
        string userGroupIdentifier,
        CreateUserGroup updateUserGroup,
        bool isInternal = false);

    /// <summary>
    /// Create a new package if the required permissions are granted, or return an error that can be returned
    /// </summary>
    /// <param name="createPackage"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer> CreatePackage(
        CreatePackage createPackage,
        bool isInternal = false);
}