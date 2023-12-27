using System.Net;
using AppliedSoftware.Models.DTOs;
using AppliedSoftware.Models.Enums;
using AppliedSoftware.Models.Request;
using AppliedSoftware.Models.Response;
using AppliedSoftware.Models.Response.PackageActions;
using AppliedSoftware.Workers;
using MimeKit;

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
    private static long[] _validIds = { 1, 2, 3, 4, 5, 6, 7, 123456, 123 };
    private static string[] _validIdentifiers = { "one", "two", "three", "four", "five", "six", "seven", "123456", "123" };
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
                new(eErrorCode.ValidationError, []));
        
        if(newTeam.BelongsToPackageId is not null && !_validIds.Contains((long) newTeam.BelongsToPackageId))
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.ValidationError, []));
        
        // Just checking permissions and validation, now that we have validated the required data is present
        // its essentially a success.
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer<IEnumerable<TeamDto>>> GetTeams(
        bool isInternal = false)
    {
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) || 
              globalPermission.HasFlag(GlobalPermission.ReadTeam)))
        {
            if (!mockUserInX)
                return new(HttpStatusCode.Forbidden, 
                    error: new());
        }
        
        return new StatusContainer<IEnumerable<TeamDto>>(
            HttpStatusCode.OK, []);
    }

    public async Task<StatusContainer<TeamDto>> GetTeam(string teamIdentifier, bool isInternal = false)
    {
        var teamIdentifierIsLong = long.TryParse(teamIdentifier, out var teamId);
        
        if(teamIdentifierIsLong && !_validIds.Contains(teamId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!teamIdentifierIsLong && !_validIdentifiers.Contains(teamIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) || 
              globalPermission.HasFlag(GlobalPermission.ReadTeam)))
        {
            if (!mockUserInX)
                return new(HttpStatusCode.Forbidden, 
                    error: new(eErrorCode.Forbidden, []));
        }
        
        return new (HttpStatusCode.OK, new ());
    }

    public async Task<StatusContainer<TeamDto>> UpdateTeam(
        string teamIdentifier, 
        CreateTeam updateTeam, 
        bool isInternal = false)
    {
        var teamIdentifierIsLong = long.TryParse(teamIdentifier, out var teamId);
        
        if(teamIdentifierIsLong && !_validIds.Contains(teamId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!teamIdentifierIsLong && !_validIdentifiers.Contains(teamIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ModifyTeam)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        // Checking to see if the team has changed data; using set values.
        if (updateTeam.HasIdenticalValues(new()
            {
                Name = "example",
                Description = "example",
                DefaultAllowedPermissions = PackageActionPermission.DefaultRead
            }))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.Conflict, []));
        
        return new(HttpStatusCode.OK, new());
    }

    public async Task<StatusContainer> DeleteTeam(string teamIdentifier, bool isInternal = false)
    {
        var teamIdentifierIsLong = long.TryParse(teamIdentifier, out var teamId);
        if(teamIdentifierIsLong && !_validIds.Contains(teamId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!teamIdentifierIsLong && !_validIdentifiers.Contains(teamIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.DeleteTeam)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer> AddUsersToTeam(string teamIdentifier, string? userIds, bool isInternal = false)
    {
        var teamIdentifierIsLong = long.TryParse(teamIdentifier, out var teamId);
        if(teamIdentifierIsLong && !_validIds.Contains(teamId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if (!teamIdentifierIsLong && !_validIdentifiers.Contains(teamIdentifier))
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, []));
        else ;
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.AddUserToTeam)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        if (string.IsNullOrWhiteSpace(userIds))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, []));

        var userIdsList = userIds.Replace(" ", "").Split(',').ToList();
        var usersInTeamAlready = new[] { "one" };
        
        // Filter out those already in the team.
        userIdsList = userIdsList.Where(x => !usersInTeamAlready.Contains(x)).ToList();
        
        if(userIdsList.Count == 0)
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.NothingToUpdate, []));
        
        var failedUsers = new List<string>();
        
        foreach (var userId in userIdsList)
        {
            if(!_validIdentifiers.Contains(userId))
                failedUsers.Add(userId);
        }
        
        if (failedUsers.Count > 0)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, []));
            
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer> RemoveUsersFromTeam(string teamIdentifier, string? userIds, bool isInternal = false)
    {
        var teamIdentifierIsLong = long.TryParse(teamIdentifier, out var teamId);
        if(teamIdentifierIsLong && !_validIds.Contains(teamId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!teamIdentifierIsLong && !_validIdentifiers.Contains(teamIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.RemoveUserFromTeam)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        
        if (string.IsNullOrWhiteSpace(userIds))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, []));

        var userIdsList = userIds.Replace(" ", "").Split(',').ToList();
        var usersInTeamAlready = new[] { "one" };
        
        // Find those who are in the team.
        userIdsList = userIdsList.Where(x => usersInTeamAlready.Contains(x)).ToList();
        
        if(userIdsList.Count == 0)
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.NothingToUpdate, []));
        
        var failedUsers = new List<string>();
        
        foreach (var userId in userIdsList)
        {
            if(!_validIdentifiers.Contains(userId))
                failedUsers.Add(userId);
        }
        
        if (failedUsers.Count > 0)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, []));
            
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer> CreateUserGroup(CreateUserGroup newUserGroup, bool isInternal = false)
    {
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.CreateUserGroup)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);

        var failedValidation = false;
        
        if (newUserGroup.TeamId is null)
            failedValidation = true;
        if(string.IsNullOrWhiteSpace(newUserGroup.Name))
            failedValidation = true;

        if (!_validIds.Contains((long)newUserGroup.TeamId!))
            failedValidation = true;
        
        if(failedValidation)
            return new(HttpStatusCode.BadRequest, 
                new(eErrorCode.ValidationError, []));
        
        // Just checking permissions and validation, now that we have validated the required data is present
        // its essentially a success.
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer<IEnumerable<UserGroupDto>>> GetUserGroups(bool isInternal = false)
    {
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ReadUserGroup)))
        {
            if(!mockUserInX)
                return new(HttpStatusCode.Forbidden,
                    error: CodeMessageResponse.ForbiddenAction);
        }
        
        return new(HttpStatusCode.OK, []);
    }

    public async Task<StatusContainer<UserGroupDto>> GetUserGroup(string userGroupIdentifier, bool isInternal = false)
    {
        var userGroupIdentifierIsLong = long.TryParse(userGroupIdentifier, out var userGroupId);
        
        if(userGroupIdentifierIsLong && !_validIds.Contains(userGroupId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!userGroupIdentifierIsLong && !_validIdentifiers.Contains(userGroupIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ReadUserGroup)))
        {
            if(!mockUserInX)
                return new(HttpStatusCode.Forbidden,
                    error: CodeMessageResponse.ForbiddenAction);
        }
        return new(HttpStatusCode.OK, new());
    }

    public async Task<StatusContainer<UserGroupDto>> UpdateUserGroup(
        string userGroupIdentifier, 
        CreateUserGroup updateUserGroup, 
        bool isInternal = false)
    {
        var userGroupIdentifierIsLong = long.TryParse(userGroupIdentifier, out var userGroupId);
        
        if(userGroupIdentifierIsLong && !_validIds.Contains(userGroupId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!userGroupIdentifierIsLong && !_validIdentifiers.Contains(userGroupIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ModifyUserGroup)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        if(updateUserGroup.HasIdenticalValues(new()
           {
               Name = "example",
               Description = "example",
               AllowedPermissions = PackageActionPermission.DefaultRead,
               DisallowedPermissions = null,
               TeamId = 1
           }))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.Conflict, []));
        
        return new(HttpStatusCode.OK, new());
    }

    public async Task<StatusContainer> DeleteUserGroup(string userGroupIdentifier, bool isInternal = false)
    {
        var userGroupIdentifierIsLong = long.TryParse(userGroupIdentifier, out var userGroupId);
        
        if(userGroupIdentifierIsLong && !_validIds.Contains(userGroupId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!userGroupIdentifierIsLong && !_validIdentifiers.Contains(userGroupIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.DeleteUserGroup)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer<IEnumerable<UserDto>?>> GetUsersInUserGroup(string userGroupIdentifier, bool isInternal = false)
    {
        var userGroupIdentifierIsLong = long.TryParse(userGroupIdentifier, out var userGroupId);
        
        if(userGroupIdentifierIsLong && !_validIds.Contains(userGroupId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!userGroupIdentifierIsLong && !_validIdentifiers.Contains(userGroupIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));

        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.DeleteUserGroup)))
        {
            if(!mockUserInX)
                return new(HttpStatusCode.Forbidden,
                    error: CodeMessageResponse.ForbiddenAction);
        }
        
        return new(HttpStatusCode.OK, []);
    }

    public async Task<StatusContainer> AddUsersToUserGroup(string userGroupIdentifier, string? userIds, bool isInternal = false)
    {
        var userGroupIdentifierIsLong = long.TryParse(userGroupIdentifier, out var userGroupId);
        if(userGroupIdentifierIsLong && !_validIds.Contains(userGroupId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!userGroupIdentifierIsLong && !_validIdentifiers.Contains(userGroupIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.AddUserToGroup)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        if (string.IsNullOrWhiteSpace(userIds))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, []));

        var userIdsList = userIds.Replace(" ", "").Split(',').ToList();
        var usersInUserGroupAlready = new[] { "one" };
        
        // Filter out those already in the team.
        userIdsList = userIdsList.Where(x => !usersInUserGroupAlready.Contains(x)).ToList();
        
        if(userIdsList.Count == 0)
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.NothingToUpdate, []));
        
        var failedUsers = new List<string>();
        
        foreach (var userId in userIdsList)
        {
            if(!_validIdentifiers.Contains(userId))
                failedUsers.Add(userId);
        }
        
        if (failedUsers.Count > 0)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, []));
            
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer> RemoveUsersFromUserGroup(string userGroupIdentifier, string? userIds, bool isInternal = false)
    {
        var userGroupIdentifierIsLong = long.TryParse(userGroupIdentifier, out var userGroupId);
        if(userGroupIdentifierIsLong && !_validIds.Contains(userGroupId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!userGroupIdentifierIsLong && !_validIdentifiers.Contains(userGroupIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.RemoveUserFromGroup)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        if (string.IsNullOrWhiteSpace(userIds))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, []));

        var userIdsList = userIds.Replace(" ", "").Split(',').ToList();
        var usersInUserGroupAlready = new[] { "one" };
        
        // Filter out those already in the team.
        userIdsList = userIdsList.Where(x => usersInUserGroupAlready.Contains(x)).ToList();
        
        if(userIdsList.Count == 0)
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.NothingToUpdate, []));
        
        var failedUsers = new List<string>();
        
        foreach (var userId in userIdsList)
        {
            if(!_validIdentifiers.Contains(userId))
                failedUsers.Add(userId);
        }
        
        if (failedUsers.Count > 0)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, []));
            
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer> CreatePackage(CreatePackage createPackage, bool isInternal = false)
    {
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.CreatePackage)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        if(string.IsNullOrWhiteSpace(createPackage.Name))
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.ValidationError, []));
        return HttpStatusCode.Created;
    }

    public async Task<StatusContainer<IEnumerable<PackageDto>>> GetPackages(bool isInternal = false)
    {
        if(!(globalPermission.HasFlag(GlobalPermission.Administrator) || 
          globalPermission.HasFlag(GlobalPermission.ReadPackage)))
        {
            if(!mockUserInX)
                return new(HttpStatusCode.Forbidden,
                    error: CodeMessageResponse.ForbiddenAction);
        }
        
        return new(HttpStatusCode.OK, []);
    }

    public async Task<StatusContainer<PackageDto>> GetPackage(string packageIdentifier, bool isInternal = false)
    {
        var packageIdentifierIsLong = long.TryParse(packageIdentifier, out var packageId);
        
        if(packageIdentifierIsLong && !_validIds.Contains(packageId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!packageIdentifierIsLong && !_validIdentifiers.Contains(packageIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ReadPackage)))
        {
            if(!mockUserInX)
                return new(HttpStatusCode.Forbidden,
                    error: CodeMessageResponse.ForbiddenAction);
        }
        
        return new(HttpStatusCode.OK, new());
    }

    public async Task<StatusContainer> CreatePackageAction(string packageIdentifier, CreatePackageAction createPackageAction, bool isInternal = false)
    {
        var packageIdentifierIsLong = long.TryParse(packageIdentifier, out var packageId);
        
        if(packageIdentifierIsLong && !_validIds.Contains(packageId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!packageIdentifierIsLong && !_validIdentifiers.Contains(packageIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ModifyPackage)))
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        
        if(createPackageAction.PackageActionType == PackageActionType.None)
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.ValidationError, []));
        return HttpStatusCode.Created;
    }

    public async Task<StatusContainer<IEnumerable<PackageActionDto>>> GetPackageActions(string packageIdentifier, bool isInternal = false)
    {
        var packageIdentifierIsLong = long.TryParse(packageIdentifier, out var packageId);
        
        if(packageIdentifierIsLong && !_validIds.Contains(packageId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!packageIdentifierIsLong && !_validIdentifiers.Contains(packageIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ReadPackage)))
        {
            if(!mockUserInX)
                return new(HttpStatusCode.Forbidden,
                    error: CodeMessageResponse.ForbiddenAction);
        }
        
        return new(HttpStatusCode.OK, []);
    }

    public async Task<StatusContainer<PackageActionDto>> GetPackageAction(string packageIdentifier, string packageActionIdentifier, bool isInternal = false)
    {
        var packageIdentifierIsLong = long.TryParse(packageIdentifier, out var packageId);
        var packageActionIdentifierIsLong = long.TryParse(packageActionIdentifier, out var packageActionId);
        
        if(packageIdentifierIsLong && !_validIds.Contains(packageId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!packageIdentifierIsLong && !_validIdentifiers.Contains(packageIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if(packageActionIdentifierIsLong && !_validIds.Contains(packageActionId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!packageActionIdentifierIsLong && !_validIdentifiers.Contains(packageActionIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ReadPackage)))
        {
            if(!mockUserInX)
                return new(HttpStatusCode.Forbidden,
                    error: CodeMessageResponse.ForbiddenAction);
        }
        
        return new(HttpStatusCode.OK, new());
    }

    public async Task<StatusContainer<ActionResponse>> ActOnPackageAction(string packageIdentifier, string packageActionIdentifier, ActPackageAction act,
        bool isInternal = false)
    {
        var isPackageUsingId = long.TryParse(packageIdentifier, out var packageId);
        var isPackageActionsUsingId = long.TryParse(packageActionIdentifier, out var packageActionId);
        var isPackageActionsUsingEnum =
            Enum.TryParse<PackageActionType>(packageActionIdentifier, true, out var packageActionType);
        
        if (!isPackageActionsUsingId && // If its not using the package action id 
            !isPackageActionsUsingEnum)
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.ValidationError, []));
        
        if(isPackageActionsUsingEnum && packageActionType == PackageActionType.None)
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.ValidationError, []));
        
        Enum.TryParse<ActAction>(act.Action, true, out var actAction);
        
        if(isPackageUsingId && !_validIds.Contains(packageId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!isPackageUsingId && !_validIdentifiers.Contains(packageIdentifier))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(isPackageActionsUsingId && !_validIds.Contains(packageActionId))
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        if(!isPackageActionsUsingId && !isPackageActionsUsingEnum)
            return new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.NotFound, []));
        
        
        if (!(globalPermission.HasFlag(GlobalPermission.Administrator) ||
              globalPermission.HasFlag(GlobalPermission.ReadPackage)))
        {
            if(!mockUserInX)
                return new(HttpStatusCode.Forbidden,
                    error: CodeMessageResponse.ForbiddenAction);
        }

        switch (actAction)
        {
            case ActAction.ViewEmail:
            case ActAction.Search:
                var searchString = act.Filter ?? act.Email?.Search;
                if(string.IsNullOrWhiteSpace(searchString))
                    return new(HttpStatusCode.BadRequest, 
                        error: new(eErrorCode.ValidationError, []));
                
                if(!packageActionPermission.HasFlag(PackageActionPermission.ReadSelf))
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAccess);
                
                return new(HttpStatusCode.OK, new());
            case ActAction.Upload:
                if (act.Email?.File is null ||
                    act.Email.File.Length == 0)
                    return new(HttpStatusCode.BadRequest,
                        error: new(eErrorCode.ValidationError, []));

                if (!packageActionPermission.HasFlag(PackageActionPermission.AddSelf))
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAction);
            {
                try
                {
                    await using var stream = new MemoryStream(act.Email.File);
                    
                    // Discard the result as it only needs to not throw an exception for it to be valid.
                    _ = await MimeMessage.LoadAsync(stream);
                    return new(HttpStatusCode.OK, new());
                }
                catch (FormatException)
                {
                    return new(HttpStatusCode.InternalServerError,
                        error: new(eErrorCode.InvalidFormat, []));
                }
            }
            case ActAction.AddAttachments:
                if (act.Email?.Attachments is null ||
                    act.Email.Attachments.Length == 0 ||
                    act.Email.EmailId is null)
                    return new(HttpStatusCode.BadRequest,
                        error: new(eErrorCode.ValidationError, []));

                // If the user doesn't have permission to update their own emails, 
                // then we can assume that they are not allowed to perform any action pertaining to updating.
                if (!packageActionPermission.HasFlag(PackageActionPermission.UpdateSelf))
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAction);
            {
                // Attempting to update an attachment that doesn't belong to them.
                if(!mockUserInX && !packageActionPermission.HasFlag(PackageActionPermission.UpdateAlt))
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAction);
                
                return new(HttpStatusCode.OK, new());
            }
            case ActAction.Remove:
                if (act.Email?.EmailId is null)
                    return new(HttpStatusCode.BadRequest, 
                        error: new(eErrorCode.ValidationError, []));
                
                if(!packageActionPermission.HasFlag(PackageActionPermission.DeleteSelf))
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAction);
                
                if(!mockUserInX && !packageActionPermission.HasFlag(PackageActionPermission.DeleteAlt))
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAction);
                
                return new(HttpStatusCode.OK, new());
            case ActAction.None: 
            default:
                return new(HttpStatusCode.BadRequest, 
                    error: new(eErrorCode.ValidationError, []));
        }
        
    }

    public async Task<StatusContainer<(Stream, string, string)>> DownloadAttachment(long attachmentId, bool isInternal = false)
    {
        return new(HttpStatusCode.OK, new());
    }
}