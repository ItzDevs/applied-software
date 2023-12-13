using System.ComponentModel;
using System.Net;
using AppliedSoftware.Extensions;
using AppliedSoftware.Models.DTOs;
using AppliedSoftware.Models.Enums;
using AppliedSoftware.Models.Request.Teams;
using AppliedSoftware.Models.Response;
using AppliedSoftware.Workers.EFCore;
using Microsoft.EntityFrameworkCore;

namespace AppliedSoftware.Workers;

public class Repository(
    ExtranetContext context,
    IAuthentication authenticationService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<Repository> logger) : IRepository
{
    /// <inheritdoc />
    public async Task<StatusContainer<GlobalPermissionDto>> GetGlobalPermissionsForUser(
        string userId)
    {
        var user = await context.GlobalPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return user is not null
            ? new(HttpStatusCode.OK, user)
            : new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[] 
                    { "No user global permissions found." }));
    }

    /// <inheritdoc />
    public async Task<StatusContainer<UserDto>> GetUser(
        string? userId,
        bool includeDisabled = false,
        bool includeDeleted = false)
    {
        logger.LogInformation($"{nameof(GetUser)} (userId={userId})");
        if (string.IsNullOrWhiteSpace(userId))
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.ValidationError, new[] 
                    { "No user id provided." }));
        
        var user = await context.Users
          .AsNoTracking()
              .Include(x => x.Teams)
              .Include(x => x.PackageAdministrator)
              .Include(x => x.UserGroups)
              .Include(x => x.UserPermissionOverrides)
          .FirstOrDefaultAsync(x => x.Uid == userId && 
                                  (includeDisabled || !x.Disabled) && 
                                  (includeDeleted  || !x.Deleted)); 

        return user is not null
          ? new(HttpStatusCode.OK, user)
            : new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound,
                    new[] { "No user found." }));
    }
    

    /// <summary>
    /// A helper method to validate that a user has a global permission entry, and pass back the permission flags
    /// (or an error) that can be returned to the user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    private async Task<StatusContainer<GlobalPermission>> ValidateUser(
        string? userId)
    {
        logger.LogInformation($"{nameof(ValidateUser)} (userId={userId})");
        if (string.IsNullOrWhiteSpace(userId))
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        

        var getUserGlobalPermissions
            = await GetGlobalPermissionsForUser(
                userId);

        if (!getUserGlobalPermissions.Success)
        {
            logger.LogWarning($"User {userId} does not have any global permissions");
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        }

        var permissionFlag = getUserGlobalPermissions.ResponseData.Body?.GrantedGlobalPermission ?? 
                             GlobalPermission.None;
        return new(HttpStatusCode.OK, permissionFlag);
    }

    /// <inheritdoc />
    public async Task<StatusContainer> CreateTeam(
        CreateTeam newTeam,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(CreateTeam)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    validation.ResponseData.Error);
            

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.CreateTeam)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to create team");
                return new(HttpStatusCode.Forbidden, 
                    CodeMessageResponse.ForbiddenAction);
            }
        }
        
        // Data validation
        if(string.IsNullOrWhiteSpace(newTeam.Name))
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.ValidationError, new[] 
                    { "A team name must be provided." }));

        try
        {
            var team = new TeamDto
            {
                Name = newTeam.Name,
                Description = newTeam.Description,
                DefaultAllowedPermissions = newTeam.DefaultAllowedPermissions ?? PackageActionPermission.None,
                PackageId = newTeam.BelongsToPackageId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            context.Teams.Add(team);
            await context.SaveChangesAsync();

            return HttpStatusCode.Created;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to create team - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.Conflict, new[]
                    { "Failed to create a team, please ensure there is no team with the same name." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create team");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create a team, please try again later." }));
        }
    }

    /// <inheritdoc />
    public async Task<StatusContainer<IEnumerable<TeamDto>>> GetTeams(
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetTeams)}");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        // This allows the service to work out if the user doesn't have permission to see any teams, or if there just are none available.
        var authorisedIfEmpty = true;
        List<TeamDto> userTeams = [];
        List<TeamDto> readTeams = []; // For if the user has read permissions for user groups as a GlobalPermission.
        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            var userInTeam 
                = await GetUser(
                    userId);
            if(!userInTeam.Success)
                return new(userInTeam.StatusCode, 
                    error: userInTeam.ResponseData.Error);
            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ReadTeam)))
            { 
                logger.LogWarning($"User {userId} does not have the required permissions to read team information");
                authorisedIfEmpty = false; 
            }
            else
            {
                readTeams = await context.Teams
                  .AsNoTracking()
                  .Where(x => !x.Deleted)
                  .ToListAsync();
            }

            userTeams = await context.Teams
                .AsNoTracking()
                .Where(x => x.Users.Any(y => y.Uid == userId) && !x.Deleted)
                .ToListAsync();
        }
        else
            readTeams = await context.Teams
             .AsNoTracking()
             .ToListAsync();

        List<TeamDto> grouped = [..userTeams, ..readTeams];
        grouped = grouped.DistinctBy(x => x.TeamId).ToList();
        return grouped.Count switch
        {
            0 when authorisedIfEmpty 
                => new(HttpStatusCode.NotFound, 
                    error: new(eErrorCode.NotFound, new[]
                        { "No user groups were found" })),
            0 => new(HttpStatusCode.Forbidden,
                error: new(eErrorCode.Forbidden, new[]
                    { "You do not have access to user groups.", "You are not in any user groups." })),
            _ => new(HttpStatusCode.OK, grouped)
        };
    }
    
    /// <inheritdoc />
    public async Task<StatusContainer<TeamDto>> GetTeam(
        string teamIdentifier,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetTeam)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        var isUsingId = long.TryParse(teamIdentifier, out var teamId);
        
        
        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userInTeam 
                = await GetUser(
                    userId);
            
            if(!userInTeam.Success)
                return new(userInTeam.StatusCode, 
                    error: userInTeam.ResponseData.Error);

            var teams = userInTeam.ResponseData.Body?.Teams;

            var teamsAsIds = teams?.Select(x => x.TeamId);
            var teamsAsNames = teams?.Select(x => x.Name);
            
            var permissionFlag = validation.ResponseData.Body;

            var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                       permissionFlag.HasFlag(GlobalPermission.ReadTeam);

            var teamFound = teamsAsIds?.Contains(teamId) == true || 
                            teamsAsNames?.Contains(teamIdentifier) == true;
            // If the user does not have the global permission Administrator or ReadTeam
            if (!flagPermissionsFound && 
                // Then we need to check if the user is a member of the requested team
                !teamFound)
            {
                logger.LogWarning($"User {userId} does not have the required permissions to read team information");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAccess);
            }
        }

        TeamDto? team;
        if(isUsingId)
            team = await context.Teams.FirstOrDefaultAsync(x => x.TeamId == teamId);
        else
            team = await context.Teams.FirstOrDefaultAsync(x => x.Name == teamIdentifier);

        return team is not null
            ? new(HttpStatusCode.OK, team)
            : new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested team ({teamIdentifier}) could not be found." }));
    }

    /// <inheritdoc />
    public async Task<StatusContainer<TeamDto>> UpdateTeam(
        string teamIdentifier,
        CreateTeam updateTeam,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(UpdateTeam)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
            {
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            }

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ModifyTeam)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to update team");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAction);
            }
        }
        
        // Checking to see if the teamIdentifier is a long (the team Id) or the team name; and attempt to load the team
        // accordingly.
        TeamDto? team;
        if(long.TryParse(teamIdentifier, out var teamId))
            team = await context.Teams.FirstOrDefaultAsync(x => x.TeamId == teamId);
        else
            team = await context.Teams.FirstOrDefaultAsync(x => x.Name == teamIdentifier);

        if (team is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested team ({teamIdentifier}) could not be found." }));
        
        // Data validation; overloaded == operator to check that the objects are the same (CreateTeam must be the first parameter).
        if (updateTeam == team)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.Conflict, new[] 
                    { "No changes detected." }));
        try
        {
            // Back any nulls with the ?? operator to fall back to the previous value.
            team.Name = updateTeam.Name ?? team.Name;
            team.Description = updateTeam.Description ?? team.Description;
            team.DefaultAllowedPermissions = updateTeam.DefaultAllowedPermissions ?? team.DefaultAllowedPermissions;
            team.PackageId = updateTeam.BelongsToPackageId ?? team.PackageId;
            team.UpdatedAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return new(HttpStatusCode.OK, 
                team);
        }
        
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not update team");
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.BadRequest, new[] 
                    { "Failed to update the team." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not update team");
            
            return new(HttpStatusCode.InternalServerError, 
                error: new(eErrorCode.ServiceUnavailable, new[] 
                { "Failed to update team.", 
                  "An unknown error occurred." }));
        }
    }

    /// <inheritdoc />
    public async Task<StatusContainer> DeleteTeam(
        string teamIdentifier,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(UpdateTeam)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
            {
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            }

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.DeleteTeam)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to delete the team");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAction);
            }
        }
        
        // Checking to see if the teamIdentifier is a long (the team Id) or the team name; and attempt to load the team
        // accordingly.
        TeamDto? team;
        if(long.TryParse(teamIdentifier, out var teamId))
            team = await context.Teams.FirstOrDefaultAsync(x => x.TeamId == teamId);
        else
            team = await context.Teams.FirstOrDefaultAsync(x => x.Name == teamIdentifier);

        if (team is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested team ({teamIdentifier}) could not be found." }));

        try
        {
            team.Deleted = true;
            team.UpdatedAtUtc = DateTime.UtcNow;
        
            await context.SaveChangesAsync();
        
            return HttpStatusCode.OK;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not delete team");
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.BadRequest, new[] 
                    { "Failed to delete the team." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not delete user group");
            
            return new(HttpStatusCode.InternalServerError, 
                error: new(eErrorCode.ServiceUnavailable, new[] 
                { "Failed to delete team.", 
                    "An unknown error occurred." }));
        }
    }

    /// <inheritdoc />
    public async Task<StatusContainer> CreateUserGroup(
        CreateUserGroup newUserGroup,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(CreateUserGroup)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    validation.ResponseData.Error);
            

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.CreateUserGroup)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to create user group");
                return new(HttpStatusCode.Forbidden, 
                    CodeMessageResponse.ForbiddenAction);
            }
        }
        
        // Data validation
        var messages = new List<string>();
        if(newUserGroup.TeamId is null)
            messages.Add("TeamId is required.");
        if(string.IsNullOrWhiteSpace(newUserGroup.Name))
            messages.Add("A user group name is required.");

        var teamExists = await context.Teams
            .FirstOrDefaultAsync(x => x.TeamId == newUserGroup.TeamId) is not null;
        
       if(!teamExists)
           messages.Add("The team does not exist.");
        
        if(messages.Count > 0)
            return new(HttpStatusCode.BadRequest, 
                new(eErrorCode.ValidationError, messages));

        try
        {
            var userGroup = new UserGroupDto
            {
                TeamId = (long)newUserGroup.TeamId!,
                Name = newUserGroup.Name!,
                Description = newUserGroup.Description,
                AllowedPermissions = newUserGroup.AllowedPermissions,
                DisallowedPermissions = newUserGroup.DisallowedPermissions,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            context.UserGroups.Add(userGroup);
            await context.SaveChangesAsync();
            
            return HttpStatusCode.Created;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to create team - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.Conflict, new[]
                    { "Failed to create a team, please ensure there is no team with the same name." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create team");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create a team, please try again later." }));
        }
    }

    /// <inheritdoc />
    public async Task<StatusContainer<IEnumerable<UserGroupDto>>> GetUserGroups(
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetUserGroups)}");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        // This allows the service to work out if the user doesn't have permission to see any user groups, or if there just are none available.
        var authorisedIfEmpty = true;
        List<UserGroupDto> userUserGroups = [];
        List<UserGroupDto> readUserGroups = []; // For if the user has read permissions for user groups as a GlobalPermission.
        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            
            var userInTeam 
                = await GetUser(
                    userId);
            if(!userInTeam.Success)
                return new(userInTeam.StatusCode, 
                    error: userInTeam.ResponseData.Error);
            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                  permissionFlag.HasFlag(GlobalPermission.ReadUserGroup)))
            { 
                logger.LogWarning($"User {userId} does not have the required permissions to view non-granted user group information");
                authorisedIfEmpty = false;
            }
               
            else
                readUserGroups = await context.UserGroups
                  .AsNoTracking()
                  .Where(x => !x.IsDeleted)
                  .ToListAsync();

            userUserGroups = await context.UserGroups
                .AsNoTracking()
                .Where(x => x.Users.Any(y => y.Uid == userId) && !x.IsDeleted)
                .ToListAsync();
        }
        else
            readUserGroups = await context.UserGroups
                .AsNoTracking()
                .ToListAsync();

        List<UserGroupDto> grouped = [..userUserGroups, ..readUserGroups];
        grouped = grouped.DistinctBy(x => x.TeamId).ToList();

        foreach (var ug in grouped)
        {
            ug.RemoveCollections();
        }
        
        return grouped.Count switch
        {
            0 when authorisedIfEmpty 
              => new(HttpStatusCode.NotFound, 
                    error: new(eErrorCode.NotFound, new[]
                        { "No user groups were found" })),
            0 => new(HttpStatusCode.Forbidden,
                    error: new(eErrorCode.Forbidden, new[]
                    { "You do not have access to user groups.", "You are not in any user groups." })),
            _ => new(HttpStatusCode.OK, grouped)
        };
    }

    /// <inheritdoc />
    public async Task<StatusContainer<UserGroupDto>> GetUserGroup(
        string userGroupIdentifier,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetUserGroup)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        var isUsingId = long.TryParse(userGroupIdentifier, out var userGroupId);
        
        
        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userInGroup 
                = await GetUser(
                    userId);
            
            if(!userInGroup.Success)
                return new(userInGroup.StatusCode, 
                    error: userInGroup.ResponseData.Error);

            var userGroups = userInGroup.ResponseData.Body?.UserGroups;

            var groupsAsIds = userGroups?.Select(x => x.UserGroupId);
            var groupsAsNames = userGroups?.Select(x => x.Name);
            
            var permissionFlag = validation.ResponseData.Body;
            var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                       permissionFlag.HasFlag(GlobalPermission.ReadTeam);

            var userGroupFound = groupsAsIds?.Contains(userGroupId) == true || 
                            groupsAsNames?.Contains(userGroupIdentifier) == true;
            // If the user does not have the global permission Administrator or ReadTeam
            if (!flagPermissionsFound && 
                // Then we need to check if the user is a member of the requested team
                !userGroupFound)
            {
                logger.LogWarning($"User {userId} does not have the required permissions to read group information");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAccess);
            }
        }

        UserGroupDto? userGroup;
        if(isUsingId)
            userGroup = await context.UserGroups.FirstOrDefaultAsync(x => x.UserGroupId == userGroupId);
        else
            userGroup = await context.UserGroups.FirstOrDefaultAsync(x => x.Name == userGroupIdentifier);

        
        
        return userGroup is not null
            ? new(HttpStatusCode.OK, userGroup.RemoveCollections())
            : new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested user group ({userGroupIdentifier}) could not be found." }));
    }

    /// <inheritdoc />
    public async Task<StatusContainer<UserGroupDto>> UpdateUserGroup(
        string userGroupIdentifier,
        CreateUserGroup updateUserGroup,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(UpdateUserGroup)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
            {
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            }

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ModifyUserGroup)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to update team");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAction);
            }
        }
        
        // Checking to see if the teamIdentifier is a long (the team Id) or the team name; and attempt to load the team
        // accordingly.
        UserGroupDto? userGroup;
        if(long.TryParse(userGroupIdentifier, out var userGroupId))
            userGroup = await context.UserGroups.FirstOrDefaultAsync(x => x.UserGroupId == userGroupId);
        else
            userGroup = await context.UserGroups.FirstOrDefaultAsync(x => x.Name == userGroupIdentifier);

        if (userGroup is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested user group ({userGroupIdentifier}) could not be found." }));
        
        // Data validation; overloaded == operator to check that the objects are the same (CreateUserGroup must be the first parameter).
        if (updateUserGroup == userGroup)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.Conflict, new[] 
                    { "No changes detected." }));

        try
        {
            // Back any nulls with the ?? operator to fall back to the previous value.
            userGroup.Name = updateUserGroup.Name ?? userGroup.Name;
            userGroup.Description = updateUserGroup.Description ?? userGroup.Description;
            userGroup.AllowedPermissions = updateUserGroup.AllowedPermissions ?? userGroup.AllowedPermissions;
            userGroup.DisallowedPermissions = updateUserGroup.DisallowedPermissions ?? userGroup.DisallowedPermissions;
            userGroup.TeamId = updateUserGroup.TeamId ?? userGroup.TeamId;
            userGroup.UpdatedAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return new(HttpStatusCode.OK, 
                userGroup);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not update user group");
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.BadRequest, new[] 
                    { "Failed to update the user group." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not update user group");
            
            return new(HttpStatusCode.InternalServerError, 
                error: new(eErrorCode.ServiceUnavailable, new[] 
                { "Failed to update user group.", 
                    "An unknown error occurred." }));
        }
    }

    public async Task<StatusContainer> DeleteUserGroup(
        string userGroupIdentifier,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(UpdateTeam)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
            {
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            }

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.DeleteUserGroup)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to delete the team");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAction);
            }
        }
        
        // Checking to see if the teamIdentifier is a long (the team Id) or the team name; and attempt to load the team
        // accordingly.
        UserGroupDto? userGroup;
        if(long.TryParse(userGroupIdentifier, out var userGroupId))
            userGroup = await context.UserGroups.FirstOrDefaultAsync(x => x.UserGroupId == userGroupId);
        else
            userGroup = await context.UserGroups.FirstOrDefaultAsync(x => x.Name == userGroupIdentifier);

        if (userGroup is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested team ({userGroupIdentifier}) could not be found." }));

        try
        {
            userGroup.IsDeleted = true;
            userGroup.UpdatedAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return HttpStatusCode.OK;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not delete user group");
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.BadRequest, new[] 
                    { "Failed to delete the user group." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not delete user group");
            
            return new(HttpStatusCode.InternalServerError, 
                error: new(eErrorCode.ServiceUnavailable, new[] 
                    { "Failed to delete user group.", 
                      "An unknown error occurred." }));
        }
    }

    public async Task<StatusContainer<IEnumerable<UserDto>?>> GetUsersInUserGroup(
        string userGroupIdentifier,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(AddUsersToUserGroup)}");
        
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        var isUsingId = long.TryParse(userGroupIdentifier, out var userGroupId);
        
        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userInGroup 
                = await GetUser(
                    userId);
            
            if(!userInGroup.Success)
                return new(userInGroup.StatusCode, 
                    error: userInGroup.ResponseData.Error);
            var userGroups = userInGroup.ResponseData.Body?.UserGroups;

            var groupsAsIds = userGroups?.Select(x => x.UserGroupId);
            var groupsAsNames = userGroups?.Select(x => x.Name);
            
            var permissionFlag = validation.ResponseData.Body;
            var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                       permissionFlag.HasFlag(GlobalPermission.ReadUserGroup);

            var userGroupFound = groupsAsIds?.Contains(userGroupId) == true || 
                                 groupsAsNames?.Contains(userGroupIdentifier) == true;
            // If the user does not have the global permission Administrator or ReadTeam
            if (!flagPermissionsFound && 
                // Then we need to check if the user is a member of the requested team
                !userGroupFound)
            {
                logger.LogWarning($"User {userId} does not have the required permissions to view user group");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAccess);
            }
        }
        
        UserGroupDto? userGroup;
        if(isUsingId)
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.UserGroupId == userGroupId);
        else
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Name == userGroupIdentifier);

        if (userGroup is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested user group ({userGroupIdentifier}) could not be found." }));

        foreach (var user in userGroup.Users)
        {
            user.RemoveCollections();
        }
        return userGroup.Users.Count > 0
            ? new(HttpStatusCode.OK,
                userGroup.Users)
            : new(HttpStatusCode.NotFound, 
                error: new(eErrorCode.EmptyResults, new[] 
                    { "There are no users in the user group." }));
    }
    
    public async Task<StatusContainer> AddUsersToUserGroup(
        string userGroupIdentifier,
        string? userIds, // Comma separated list of user ids
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(AddUsersToUserGroup)}");
        
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    validation.ResponseData.Error);

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ModifyUserGroup)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to update a user group");
                return new(HttpStatusCode.Forbidden, 
                    CodeMessageResponse.ForbiddenAction);
            }
        }

        if (string.IsNullOrWhiteSpace(userIds))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, new []
                    { "User ids cannot be empty." }));
        
        var userIdsList = userIds.Replace(" ", "").Split(',').ToList();

        UserGroupDto? userGroup;
        if(long.TryParse(userGroupIdentifier, out var userGroupId))
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.UserGroupId == userGroupId);
        else
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Name == userGroupIdentifier);

        if (userGroup is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested user group ({userGroupIdentifier}) could not be found." }));
        var currentUsersInGroup = userGroup.Users.Select(x => x.Uid).ToList();
        // Filter out any users that are already in the group.
        userIdsList = userIdsList.Where(x => !currentUsersInGroup.Contains(x)).ToList();

        // Check that we have some users to add.
        if (userIdsList.Count == 0) 
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.NothingToUpdate, new[]
                    { "All the users were already in the user group." }));
        var failedUsers = new List<string>();
        try
        {
            foreach (var userId in userIdsList)
            {
                var user = await context.Users.FirstOrDefaultAsync(x => x.Uid == userId);
                // If the user id isn't in the database we add it to our failed users list.
                if (user is null)
                {
                    logger.LogWarning($"User with id {userId} was not found.");
                    failedUsers.Add(userId);
                    continue;
                }

                user.UserGroups.Add(userGroup);
            }
            await context.SaveChangesAsync();

            if (failedUsers.Count > 0)
            {
                return new(HttpStatusCode.BadRequest, 
                    error: new(eErrorCode.ValidationError, 
                        failedUsers.Select(x => $"User with id {x} was not found.").ToArray()));
            }
            return HttpStatusCode.OK;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not add users to user group");
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.BadRequest, new[]
                    { "Failed to add users to the user group." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not add users to user group");
            return new(HttpStatusCode.InternalServerError,
                error: new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to add users to the user group.",
                      "An unknown error occurred." }));
        }
    }
    
    public async Task<StatusContainer> RemoveUsersFromUserGroup(
        string userGroupIdentifier,
        string? userIds, // Comma separated list of user ids
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(AddUsersToUserGroup)}");
        
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    validation.ResponseData.Error);

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ModifyUserGroup)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to update a user group");
                return new(HttpStatusCode.Forbidden, 
                    CodeMessageResponse.ForbiddenAction);
            }
        }

        if (string.IsNullOrWhiteSpace(userIds))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, new []
                    { "User ids cannot be empty." }));
        
        var userIdsList = userIds.Replace(" ", "").Split(',').ToList();

        UserGroupDto? userGroup;
        if(long.TryParse(userGroupIdentifier, out var userGroupId))
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.UserGroupId == userGroupId);
        else
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Name == userGroupIdentifier);

        if (userGroup is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested user group ({userGroupIdentifier}) could not be found." }));
        var currentUsersInGroup = userGroup.Users.Select(x => x.Uid).ToList();
        // Filter out any users that are already in the group.
        userIdsList = userIdsList.Where(x => currentUsersInGroup.Contains(x)).ToList();

        // Check that we have some users to remove.
        if (userIdsList.Count == 0) 
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.NothingToUpdate, new[]
                    { "None of the users were in the group." }));
        var failedUsers = new List<string>();
        try
        {
            foreach (var userId in userIdsList)
            {
                var user = await context.Users.FirstOrDefaultAsync(x => x.Uid == userId);
                // If the user id isn't in the database we add it to our failed users list.
                if (user is null)
                {
                    logger.LogWarning($"User with id {userId} was not found.");
                    failedUsers.Add(userId);
                    continue;
                }

                user.UserGroups.Remove(userGroup);
            }
            await context.SaveChangesAsync();

            if (failedUsers.Count > 0)
            {
                return new(HttpStatusCode.BadRequest, 
                    error: new(eErrorCode.ValidationError, 
                        failedUsers.Select(x => $"User with id {x} was not found.").ToArray()));
            }
            return HttpStatusCode.OK;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not remove users from user group");
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.BadRequest, new[]
                    { "Failed to add users to the user group." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not add users to user group");
            return new(HttpStatusCode.InternalServerError,
                error: new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to add users to the user group.",
                      "An unknown error occurred." }));
        }
    }
    
    public async Task<StatusContainer> CreateUserGroupOverride()
    {
        throw new NotImplementedException();
    }
    
    public async Task<StatusContainer> CreatePackage(
        CreatePackage createPackage,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(CreatePackage)}");
        
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);

        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    validation.ResponseData.Error);
            

            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.CreatePackage)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to create package");
                return new(HttpStatusCode.Forbidden, 
                    CodeMessageResponse.ForbiddenAction);
            }
        }
        
        // Data validation
        if(string.IsNullOrWhiteSpace(createPackage.Name))
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.ValidationError, new[] 
                    { "A package name must be provided." }));

        try
        {
            var package = new PackageDto()
            {
                Name = createPackage.Name,
                Description = createPackage.Description,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            context.Packages.Add(package);
            await context.SaveChangesAsync();

            return HttpStatusCode.Created;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to create team - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.Conflict, new[]
                    { "Failed to create a team, please ensure there is no team with the same name." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create team");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create a team, please try again later." }));
        }
    }

    public async Task<StatusContainer<IEnumerable<PackageDto>>> GetPackages(
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetPackages)}");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        // This allows the service to work out if the user doesn't have permission to see any teams, or if there just are none available.
        var authorisedIfEmpty = true;
        List<PackageDto> userPackages = [];
        List<PackageDto> readPackages = []; // For if the user has read permissions for user groups as a GlobalPermission.
        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            var userInTeam 
                = await GetUser(
                    userId);
            if(!userInTeam.Success)
                return new(userInTeam.StatusCode, 
                    error: userInTeam.ResponseData.Error);
            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ReadPackage)))
            { 
                logger.LogWarning($"User {userId} does not have the required permissions to read package information");
                authorisedIfEmpty = false; 
            }
            else
            {
                readPackages = await context.Packages
                  .AsNoTracking()
                  .ToListAsync();
            }

            userPackages = await context.Packages
                .AsNoTracking()
                .Where(x => x.Administrators.Any(y => y.Uid == userId) || 
                            x.Teams.Any(y => y.Users.Any(z => z.Uid == userId)) ||
                            x.Actions.Any(y => y.UserPermissionOverrides.Any(z => z.User.Uid == userId) || 
                                               y.TeamPermissionOverrides.Any(z => z.UserGroup.Users.Any(w => w.Uid == userId))))
                .ToListAsync();
        }
        else
            readPackages = await context.Packages
             .AsNoTracking()
             .ToListAsync();

        List<PackageDto> grouped = [..userPackages, ..readPackages];
        grouped = grouped.DistinctBy(x => x.PackageId).ToList();

        foreach (var package in grouped)
        {
            package.RemoveCollections();
        }
        return grouped.Count switch
        {
            0 when authorisedIfEmpty 
                => new(HttpStatusCode.NotFound, 
                    error: new(eErrorCode.NotFound, new[]
                        { "No user groups were found" })),
            0 => new(HttpStatusCode.Forbidden,
                error: new(eErrorCode.Forbidden, new[]
                    { "You do not have access to user groups.", "You are not in any user groups." })),
            _ => new(HttpStatusCode.OK, grouped)
        };
    }
}