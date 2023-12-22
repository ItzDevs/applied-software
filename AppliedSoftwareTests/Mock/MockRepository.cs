using System.Net;
using AppliedSoftware.Models.DTOs;
using AppliedSoftware.Models.Enums;
using AppliedSoftware.Models.Request;
using AppliedSoftware.Models.Response;
using AppliedSoftware.Models.Response.PackageActions;
using AppliedSoftware.Workers;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftwareTests.Mock;

/// <summary>
/// 
/// </summary>
/// <param name="globalPermission"></param>
/// <param name="packageActionPermission"></param>
/// <param name="mockUserInX">Whether the responses should
/// pretend that the user has permission to view from being a member.</param>
public class MockRepository(
    // These permissions are passed in so that a lot of the time checking the user id is not needed; this gives more control
    // over the tests. hayley
    GlobalPermission globalPermission,
    PackageActionPermission packageActionPermission,
    bool mockUserInX = false)
    : IRepository
{
    // These allow checks against valid and invalid data which use XIdentifier parameters.
    private static long[] ValidIds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, };
    private static string[] ValidIdentifiersEmails = { "one", "two", "three", "four", "five", "six", "seven" };
    // Mock repository does not return any data;
    // only check against the HTTP status code + error code.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<StatusContainer<GlobalPermissionDto>> GetGlobalPermissionsForUser(string userId)
    {
        // The valid user for tests is 123456
        if(userId != "123456")
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        return new(HttpStatusCode.OK, new());
    }

    public async Task<StatusContainer<UserDto>> GetUser(string userId, bool includeDisabled = false, bool includeDeleted = false)
    {
        if(userId != "123456")
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        return new(HttpStatusCode.OK, new());
    }

    public async Task<StatusContainer> CreateTeam(CreateTeam newTeam, bool isInternal = false)
    {
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.CreateTeam)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        if(string.IsNullOrWhiteSpace(newTeam.Name))
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.ValidationError, new[] 
                    { "A team name must be provided." }));
        
        // Just checking permissions and validation, now that we have validated the required data is present
        // its essentially a success.
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer<IEnumerable<TeamDto>>> GetTeams(
        bool isInternal = false)
    {
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) || 
              globalPermission.HasFlag(GlobalPermission.ReadTeam)));
        {
            if (!mockUserInX)
                return new(HttpStatusCode.Forbidden, 
                    error: new());
        }
        
        return new StatusContainer<IEnumerable<TeamDto>>(
            HttpStatusCode.OK, new List<TeamDto>());
    }

    public async Task<StatusContainer<TeamDto>> GetTeam(string teamIdentifier, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<TeamDto>> UpdateTeam(string teamIdentifier, CreateTeam updateTeam, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> DeleteTeam(string teamIdentifier, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> AddUsersToTeam(string teamIdentifier, string? userIds, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> RemoveUsersFromTeam(string teamIdentifier, string? userIds, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> CreateUserGroup(CreateUserGroup newUserGroup, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<IEnumerable<UserGroupDto>>> GetUserGroups(bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<UserGroupDto>> GetUserGroup(string userGroupIdentifier, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<UserGroupDto>> UpdateUserGroup(string userGroupIdentifier, CreateUserGroup updateUserGroup, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> DeleteUserGroup(string userGroupIdentifier, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<IEnumerable<UserDto>?>> GetUsersInUserGroup(string userGroupIdentifier, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> AddUsersToUserGroup(string userGroupIdentifier, string? userIds, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> RemoveUsersFromUserGroup(string userGroupIdentifier, string? userIds, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> CreatePackage(CreatePackage createPackage, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<IEnumerable<PackageDto>>> GetPackages(bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<PackageDto>> GetPackage(string packageIdentifier, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer> CreatePackageAction(string packageIdentifier, CreatePackageAction createPackageAction, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<IEnumerable<PackageActionDto>>> GetPackageActions(string packageIdentifier, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<PackageActionDto>> GetPackageAction(string packageIdentifier, string packageActionIdentifier, bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<ActionResponse>> ActOnPackageAction(string packageIdentifier, string packageActionIdentifier, ActPackageAction act,
        bool isInternal = false)
    {
        throw new NotImplementedException();
    }

    public async Task<StatusContainer<(Stream, string, string)>> DownloadAttachment(long attachmentId, bool isInternal = false)
    {
        throw new NotImplementedException();
    }
}