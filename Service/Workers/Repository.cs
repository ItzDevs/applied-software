using System.Net;
using System.Security.Claims;
using AppliedSoftware.Exceptions;
using AppliedSoftware.Extensions;
using AppliedSoftware.Models.Validators;
using AppliedSoftware.Models.DTOs;
using AppliedSoftware.Models.Enums;
using AppliedSoftware.Models.Request;
using AppliedSoftware.Models.Response;
using AppliedSoftware.Models.Response.PackageActions;
using AppliedSoftware.Workers.EFCore;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace AppliedSoftware.Workers;

public class Repository(
    ExtranetContext context,
    IAuthentication authenticationService,
    IHttpContextAccessor httpContextAccessor,
    Settings settings,
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
                .ThenInclude(x => x.Package)
                .ThenInclude(x => x.Actions)
                .ThenInclude(x => x.UserGroupPermissionOverrides)
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
    /// <param name="claims"></param>
    /// <returns></returns>
    private async Task<StatusContainer<ValidatedUser>> ValidateUser(
        ClaimsPrincipal? claims)
    {
        var userId = authenticationService.ExtractUserId(claims);
        
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
        return new(HttpStatusCode.OK, new(userId, permissionFlag));
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
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
                DefaultAllowedPermissions = newTeam.DefaultAllowedPermissions ?? PackageUserPermission.None,
                PackageId = newTeam.BelongsToPackageId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await context.Teams.AddAsync(team);
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;

            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ReadTeam)))
            { 
                logger.LogWarning($"User {userId} does not have the required permissions to read team information");
                authorisedIfEmpty = false; 
            }
            else
                readTeams = await context.Teams
                  .AsNoTracking()
                  .Where(x => !x.Deleted)
                  .ToListAsync();
            

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
        
        TeamDto? team;
        if(long.TryParse(teamIdentifier, out var teamId))
            team = await context.Teams
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.TeamId == teamId);
        else
            team = await context.Teams
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Name == teamIdentifier);

        if (team is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested team ({teamIdentifier}) was not found." }));

        if (isInternal) 
            return new(HttpStatusCode.OK, team);
        
        var validation = await ValidateUser(claims);
        if (!validation.Success || validation.ResponseData.Body is null)
            return new(validation.StatusCode, 
                error: validation.ResponseData.Error);

        var userId = validation.ResponseData.Body.UserId;
        var permissionFlag = validation.ResponseData.Body.PermissionFlag;

        var userInTeam 
            = team.Users.FirstOrDefault(x => x.Uid == userId);

        var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                   permissionFlag.HasFlag(GlobalPermission.ReadTeam);
        // If the user does not have the global permission Administrator or ReadTeam
        if (flagPermissionsFound ||
            // Then we need to check if the user is a member of the requested team
            userInTeam is not null)
            return new(HttpStatusCode.OK, team);
        
        logger.LogWarning($"User {userId} does not have the required permissions to read team information");
        return new(HttpStatusCode.Forbidden, 
            error: CodeMessageResponse.ForbiddenAccess);
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
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
        if (updateTeam.HasIdenticalValues(team))
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            
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
    public async Task<StatusContainer> AddUsersToTeam(
        string teamIdentifier,
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.AddUserToTeam)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to update a team");
                return new(HttpStatusCode.Forbidden, 
                    CodeMessageResponse.ForbiddenAction);
            }
        }

        if (string.IsNullOrWhiteSpace(userIds))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, new []
                    { "User ids cannot be empty." }));
        
        var userIdsList = userIds.Replace(" ", "").Split(',').ToList();

        TeamDto? team;
        if(long.TryParse(teamIdentifier, out var teamId))
            team = await context.Teams
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.TeamId == teamId);
        else
            team = await context.Teams
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Name == teamIdentifier);

        if (team is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested team ({teamIdentifier}) could not be found." }));
        var currentUsersInTeam = team.Users.Select(x => x.Uid).ToList();
        // Filter out any users that are already in the group.
        userIdsList = userIdsList.Where(x => !currentUsersInTeam.Contains(x)).ToList();

        // Check that we have some users to add.
        if (userIdsList.Count == 0) 
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.NothingToUpdate, new[]
                    { "All the users were already in the team." }));
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

                user.Teams.Add(team);
            }
            await context.SaveChangesAsync();

            if (failedUsers.Count > 0)
                return new(HttpStatusCode.BadRequest, 
                    error: new(eErrorCode.ValidationError, 
                        failedUsers.Select(x => $"User with id {x} was not found.").ToArray()));
            
            return HttpStatusCode.OK;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Could not add users to team");
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.BadRequest, new[]
                    { "Failed to add users to the team." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not add users to team");
            return new(HttpStatusCode.InternalServerError,
                error: new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to add users to the team.",
                      "An unknown error occurred." }));
        }
    }
    
    /// <inheritdoc />
    public async Task<StatusContainer> RemoveUsersFromTeam(
        string teamIdentifier,
        string? userIds, // Comma separated list of user ids
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(RemoveUsersFromUserGroup)}");
        
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);

        if (!isInternal)
        {
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.RemoveUserFromTeam)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to update the team");
                return new(HttpStatusCode.Forbidden, 
                    CodeMessageResponse.ForbiddenAction);
            }
        }

        if (string.IsNullOrWhiteSpace(userIds))
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, new []
                    { "User ids cannot be empty." }));
        
        var userIdsList = userIds.Replace(" ", "").Split(',').ToList();

        TeamDto? team;
        if(long.TryParse(teamIdentifier, out var teamId))
            team = await context.Teams
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.TeamId == teamId);
        else
            team = await context.Teams
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Name == teamIdentifier);

        if (team is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested team ({teamIdentifier}) could not be found." }));
        var currentUsersInTeam = team.Users.Select(x => x.Uid).ToList();
        // Filter out any users that are already in the group.
        userIdsList = userIdsList.Where(x => currentUsersInTeam.Contains(x)).ToList();

        // Check that we have some users to remove.
        if (userIdsList.Count == 0) 
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.NothingToUpdate, new[]
                    { "None of the users were in the team." }));
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

                user.Teams.Remove(team);
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
            logger.LogError(ex, "Could not remove users from team");
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.BadRequest, new[]
                    { "Failed to add users to the team." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not add users to team");
            return new(HttpStatusCode.InternalServerError,
                error: new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to add users to the team.",
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            
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
            await context.UserGroups.AddAsync(userGroup);
            await context.SaveChangesAsync();
            return HttpStatusCode.Created;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to create user group - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.Conflict, new[]
                    { "Failed to create user group, please ensure there is no user group with the same name." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create user group");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create a user group, please try again later." }));
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            
            var userInTeam 
                = await GetUser(
                    userId);
            if(!userInTeam.Success)
                return new(userInTeam.StatusCode, 
                    error: userInTeam.ResponseData.Error);
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
            ug.RemoveNavigationProperties();
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

        UserGroupDto? userGroup;
        if(long.TryParse(userGroupIdentifier, out var userGroupId))
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.UserGroupId == userGroupId);
        else
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Name == userGroupIdentifier);
        
        if(userGroup is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { "The user group was not found" }));

        if (isInternal)
            return new(HttpStatusCode.OK,
                userGroup.RemoveNavigationProperties());
        
        var validation = await ValidateUser(claims);
        if (!validation.Success || validation.ResponseData.Body is null)
            return new(validation.StatusCode, 
                error: validation.ResponseData.Error);

        var userId = validation.ResponseData.Body.UserId;
        var permissionFlag = validation.ResponseData.Body.PermissionFlag;

        var userInGroup = userGroup.Users.FirstOrDefault(x => x.Uid == userId);
            
        var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                   permissionFlag.HasFlag(GlobalPermission.ReadUserGroup);

        // If the user does not have the global permission Administrator or ReadTeam
        if (flagPermissionsFound ||
            // Then we need to check if the user is a member of the requested team
            userInGroup is not null)
            return new(HttpStatusCode.OK,
                userGroup.RemoveNavigationProperties());
        
        logger.LogWarning($"User {userId} does not have the required permissions to read group information");
        return new(HttpStatusCode.Forbidden, 
            error: CodeMessageResponse.ForbiddenAccess);
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            
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
        
        if (updateUserGroup.HasIdenticalValues(userGroup))
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

    
    /// <inheritdoc />
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            
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

    /// <inheritdoc />
    public async Task<StatusContainer<IEnumerable<UserDto>?>> GetUsersInUserGroup(
        string userGroupIdentifier,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetUsersInUserGroup)}");
        
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        UserGroupDto? userGroup;
        if(long.TryParse(userGroupIdentifier, out var userGroupId))
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.UserGroupId == userGroupId);
        else
            userGroup = await context.UserGroups
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Name == userGroupIdentifier);

        if(userGroup is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested user group ({userGroupIdentifier}) could not be found." }));
        
        if (!isInternal)
        {
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode,
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;

            var userInGroup = userGroup.Users.FirstOrDefault(x => x.Uid == userId);

            var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                       permissionFlag.HasFlag(GlobalPermission.ReadUserGroup);

            // If the user does not have the global permission Administrator or ReadUserGroup
            if (flagPermissionsFound ||
                // Then we need to check if the user is a member of the requested user group
                userInGroup is not null)
            {
                foreach (var user in userGroup.Users)
                {
                    user.RemoveNavigationProperties();
                }

                return userGroup.Users.Count > 0 ?
                    new(HttpStatusCode.OK, userGroup.Users) :
                    new(HttpStatusCode.NotFound, error: new(eErrorCode.NotFound, new[] 
                        { "No users were found in the user group." }));
            }

            logger.LogWarning($"User {userId} does not have the required permissions to view user group");
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAccess);
        }
        foreach (var user in userGroup.Users)
        {
            user.RemoveNavigationProperties();
        }

        return userGroup.Users.Count > 0 ?
            new(HttpStatusCode.OK, userGroup.Users) :
            new(HttpStatusCode.NotFound, error: new(eErrorCode.NotFound, new[] 
                { "No users were found in the user group." }));
    }
    
    /// <inheritdoc />
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.AddUserToGroup)))
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
                return new(HttpStatusCode.BadRequest, 
                    error: new(eErrorCode.ValidationError, 
                        failedUsers.Select(x => $"User with id {x} was not found.").ToArray()));
            
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
    
    /// <inheritdoc />
    public async Task<StatusContainer> RemoveUsersFromUserGroup(
        string userGroupIdentifier,
        string? userIds, // Comma separated list of user ids
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(RemoveUsersFromUserGroup)}");
        
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);

        if (!isInternal)
        {
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.RemoveUserFromGroup)))
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
    
    /// <inheritdoc />
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
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
            var package = new PackageDto
            {
                Name = createPackage.Name,
                Description = createPackage.Description,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            await context.Packages.AddAsync(package);
            await context.SaveChangesAsync();

            return HttpStatusCode.Created;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to create package - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.Conflict, new[]
                    { "Failed to create the package, please ensure there is no package with the same name." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create package");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create the package, please try again later." }));
        }
    }

    /// <inheritdoc />
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
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            var userInTeam 
                = await GetUser(
                    userId);
            
            if(!userInTeam.Success)
                return new(userInTeam.StatusCode, 
                    error: userInTeam.ResponseData.Error);
            
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
                                               y.UserGroupPermissionOverrides.Any(z => z.UserGroup.Users.Any(w => w.Uid == userId))))
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
            package.RemoveNavigationProperties();
        }
        return grouped.Count switch
        {
            0 when authorisedIfEmpty 
                => new(HttpStatusCode.NotFound, 
                    error: new(eErrorCode.NotFound, new[]
                        { "No packages were found" })),
            0 => new(HttpStatusCode.Forbidden,
                error: new(eErrorCode.Forbidden, new[]
                    { "You do not have access to packages.", 
                        "You are not a member of any packages." })),
            _ => new(HttpStatusCode.OK, grouped)
        };
    }

    /// <inheritdoc />
    public async Task<StatusContainer<PackageDto>> GetPackage(
        string packageIdentifier,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetPackage)} (packageIdentifier={packageIdentifier}; isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        var isUsingId = long.TryParse(packageIdentifier, out var packageId);
        
        PackageDto? package;
        if(isUsingId)
            package = await context.Packages.FirstOrDefaultAsync(x => x.PackageId == packageId);
        else
            package = await context.Packages.FirstOrDefaultAsync(x => x.Name == packageIdentifier);
        
        if (package is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { "The package was not found." }));

        if (isInternal)
            return new(HttpStatusCode.OK, package.RemoveNavigationProperties());
        
        var validation = await ValidateUser(claims);
        if (!validation.Success || validation.ResponseData.Body is null)
            return new(validation.StatusCode, 
                error: validation.ResponseData.Error);

        var userId = validation.ResponseData.Body.UserId;
        var permissionFlag = validation.ResponseData.Body.PermissionFlag;

        var userInPackage
            = package.UserInPackage(userId);
        

        var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                   permissionFlag.HasFlag(GlobalPermission.ReadPackage);

        // If the user does not have the global permission Administrator or ReadPackage
        if (flagPermissionsFound ||
            // Then we need to check if the user is a member of the requested package
            userInPackage)
            return new(HttpStatusCode.OK, package.RemoveNavigationProperties());
        
        logger.LogWarning($"User {userId} does not have the required permissions to read package information");
        return new(HttpStatusCode.Forbidden, 
            error: CodeMessageResponse.ForbiddenAccess);
    }

    /// <inheritdoc />
    public async Task<StatusContainer> CreatePackageAction(
        string packageIdentifier,
        CreatePackageAction newPackageAction,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(CreatePackageAction)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        

        if (!isInternal)
        {
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ModifyPackage)))
            {
                logger.LogWarning($"User {userId} does not have the required permissions to update package (package actions)");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAction);
            }
        }
        
        var messages = new List<string>();
        if(string.IsNullOrWhiteSpace(packageIdentifier))
            messages.Add("Package Id is required.");
        if(newPackageAction.PackageActionType == PackageActionType.None)
            messages.Add("The action type must be defined.");

        var isUsingId = long.TryParse(packageIdentifier, out var packageId);
        
        var package = isUsingId ? 
            await context.Packages.FirstOrDefaultAsync(x => x.PackageId == packageId) : 
            await context.Packages.FirstOrDefaultAsync(x => x.Name == packageIdentifier);
        
        if(package is null)
            messages.Add("The package does not exist.");
        
        if(messages.Count > 0)
            return new(HttpStatusCode.BadRequest, 
                new(eErrorCode.ValidationError, messages));

        try
        {
            var packageAction = new PackageActionDto
            {
                PackageId = (long) package?.PackageId!,
                PackageActionType = newPackageAction.PackageActionType,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await context.PackageActions.AddAsync(packageAction);
            await context.SaveChangesAsync();
            return HttpStatusCode.Created;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to create package action - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.Conflict, new[]
                    { "Failed to create the package action, please ensure that its not duplicated." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create package action");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create the package action, please try again later." }));
        }
    }

    /// <inheritdoc />
    public async Task<StatusContainer<IEnumerable<PackageActionDto>>> GetPackageActions(
        string packageIdentifier,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetPackageActions)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        var isUsingId = long.TryParse(packageIdentifier, out var packageId);
        if (!isInternal)
        {
            var validation = await ValidateUser(claims);
            if (!validation.Success || validation.ResponseData.Body is null)
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);

            var userId = validation.ResponseData.Body.UserId;
            var permissionFlag = validation.ResponseData.Body.PermissionFlag;
            
            var userInPackage 
                = await GetUser(
                    userId);
            
            if(!userInPackage.Success)
                return new(userInPackage.StatusCode, 
                    error: userInPackage.ResponseData.Error);

            var packages = userInPackage.ResponseData.Body?.GetPackages().ToList();

            var packagesAsIds = packages?.Select(x => x.PackageId);
            var packagesAsNames = packages?.Select(x => x.Name);

            var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                       permissionFlag.HasFlag(GlobalPermission.ReadPackage);

            var packageFound = packagesAsIds?.Contains(packageId) == true || 
                               packagesAsNames?.Contains(packageIdentifier) == true;
            // If the user does not have the global permission Administrator or ReadPackage
            if (!flagPermissionsFound && 
                // Then we need to check if the user is a member of the requested package
                !packageFound)
            {
                logger.LogWarning($"User {userId} does not have the required permissions to read the package (package actions)");
                return new(HttpStatusCode.Forbidden, 
                    error: CodeMessageResponse.ForbiddenAction);
            }
        }

        PackageDto? package;
        if(isUsingId)
            package = await context.Packages
                .Include(x => x.Actions)
                .FirstOrDefaultAsync(x => x.PackageId == packageId);
        else
            package = await context.Packages
                .Include(x => x.Actions)
                .FirstOrDefaultAsync(x => x.Name == packageIdentifier);

        if(package is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested package ({packageIdentifier}) could not be found." }));
        
        var packageActions = package.Actions;
        foreach (var packageAction in packageActions)
        {
            packageAction.RemoveNavigationProperties();
        }
        
        return packageActions.Count > 0 ? 
            new(HttpStatusCode.OK, packageActions) : 
            new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested package ({packageIdentifier}) could not be found." }));
    }

    public async Task<StatusContainer<PackageActionDto>> GetPackageAction(
        string packageIdentifier,
        string packageActionIdentifier,
        bool isInternal = false) // Email / Search / Id
    {
        logger.LogInformation($"{nameof(GetPackageAction)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        
        var isPackageUsingId = long.TryParse(packageIdentifier, out var packageId);
        var isPackageActionsUsingId = long.TryParse(packageActionIdentifier, out var packageActionId);

        var isPackageActionsUsingEnum =
            Enum.TryParse<PackageActionType>(packageActionIdentifier, true, out var packageActionType);
        
        if (!isPackageActionsUsingId && // If its not using the package action id 
            !isPackageActionsUsingEnum)
        {
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.ValidationError, new[]
                    { "The package action identifier is not valid." }));
        }
        
        PackageActionDto? packageAction;
        if(isPackageActionsUsingId)
            packageAction = await context.PackageActions
                .Include(x => x.Package)
                .FirstOrDefaultAsync(x => x.PackageActionId == packageActionId);
        else if (isPackageUsingId)
            packageAction = await context.PackageActions
                .Include(x => x.Package)
                .FirstOrDefaultAsync(x => x.Package.PackageId == packageId &&
                                          x.PackageActionType == packageActionType);
        else
            packageAction = await context.PackageActions
                .Include(x => x.Package)
                .FirstOrDefaultAsync(x => x.Package.Name == packageIdentifier &&
                                          x.PackageActionType == packageActionType);

        if(packageAction is null || 
           // Not only checking if the package exists, but also that the provided packageIdentifier in 
           // the endpoint is correct for the package action.
           (packageAction.Package.PackageId != packageId && 
            !packageAction.Package.Name.Equals(packageIdentifier)))
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested package action ({packageActionIdentifier}) for the package ({packageIdentifier}) could not be found." }));
        
        if (isInternal) 
            return new(HttpStatusCode.OK, packageAction.RemoveNavigationProperties());
        
        var validation = await ValidateUser(claims);
        if (!validation.Success || validation.ResponseData.Body is null)
            return new(validation.StatusCode, 
                error: validation.ResponseData.Error);

        var userId = validation.ResponseData.Body.UserId;
        var permissionFlag = validation.ResponseData.Body.PermissionFlag;


        var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                   permissionFlag.HasFlag(GlobalPermission.ReadPackage);
        packageAction.UserInPackageAction(userId, out var actingPermissions);

        if (flagPermissionsFound)
        {
            logger.LogInformation("Getting package actions using global permissions");
            return new(HttpStatusCode.OK, packageAction.RemoveNavigationProperties());
        }

        if (actingPermissions is null)
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAccess);
        
        var nonNullActingPermissions = (PackageUserPermission) actingPermissions;

        if (nonNullActingPermissions.HasFlag(PackageUserPermission.ViewAction))
            return new(HttpStatusCode.OK, packageAction.RemoveNavigationProperties());
         
        logger.LogWarning($"User {userId} does not have the required permissions to read the package (package actions)");
        return new(HttpStatusCode.Forbidden, 
            error: CodeMessageResponse.ForbiddenAccess);
    }

    public async Task<StatusContainer<ActionResponse>> ActOnPackageAction(
        string packageIdentifier, 
        string packageActionIdentifier, 
        ActPackageAction act, 
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetPackageAction)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        
        var isPackageUsingId = long.TryParse(packageIdentifier, out var packageId);
        var isPackageActionsUsingId = long.TryParse(packageActionIdentifier, out var packageActionId);
        var isPackageActionsUsingEnum =
            Enum.TryParse<PackageActionType>(packageActionIdentifier, true, out var packageActionType);
        
        if (!isPackageActionsUsingId && // If its not using the package action id 
            !isPackageActionsUsingEnum)
            return new(HttpStatusCode.BadRequest,
                error: new(eErrorCode.ValidationError, new[]
                    { "The package action identifier is not valid." }));
        
        Enum.TryParse<ActAction>(act.Action, true, out var actAction);
        
        var packageAction 
            = await context.PackageActions
                .Include(x => x.Package) 
                 .ThenInclude(x => x.Teams)
                  .ThenInclude(x => x.UserGroups)
                   .ThenInclude(x => x.Users)
                .Include(x => x.Package)
                 .ThenInclude(x => x.Teams)
                  .ThenInclude(x => x.Users)
                .Include(x => x.Package)
                 .ThenInclude(x => x.Administrators)
                .FirstOrDefaultAsync(x => (!isPackageActionsUsingId || x.PackageActionId == packageActionId) &&
                                          (!isPackageUsingId || x.Package.PackageId == packageId &&
                                                    x.PackageActionType == packageActionType) && 
                                          (isPackageActionsUsingEnum || x.Package.Name == packageIdentifier && 
                                                    x.PackageActionType == packageActionType));
       
        if(packageAction is null || 
           // Not only checking if the package exists, but also that the provided packageIdentifier in 
           // the endpoint is correct for the package action.
           (packageAction.Package.PackageId != packageId && 
            !packageAction.Package.Name.Equals(packageIdentifier)))
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[]
                    { $"The requested package action ({packageActionIdentifier}) for the package ({packageIdentifier}) could not be found." }));

        

        var validation = await ValidateUser(claims);
        if(!validation.Success || validation.ResponseData.Body is null)
            return new(validation.StatusCode, 
                error: validation.ResponseData.Error);

        var userId = validation.ResponseData.Body.UserId;
        var permissionFlag = validation.ResponseData.Body.PermissionFlag;
        
        var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                   permissionFlag.HasFlag(GlobalPermission.ReadPackage);
        var userInPackage = packageAction.UserInPackageAction(userId, out var actingPermissions);
        
        // First layer of permission checks.
        if (!isInternal && 
            (!flagPermissionsFound && 
             !userInPackage))
        {
            logger.LogWarning($"User {userId} does not have the required permissions to run acts on the package action ({packageActionIdentifier})");
            return new(HttpStatusCode.Forbidden, 
                error: CodeMessageResponse.ForbiddenAction);
        }
        
        var valid = false;
        ActionResponse? actionResponse = null;
        switch (actAction)
        {
            // Current Search internally redirects to ViewEmail
            case ActAction.Search:
            case ActAction.ViewEmail:
                var searchString = act.Filter ?? act.Email?.Search;
                if(string.IsNullOrWhiteSpace(searchString))
                    break;
                try
                {
                    var emails
                        = await GetEmails(
                            packageAction, 
                            searchString, 
                            userId, 
                            permissionFlag, 
                            actingPermissions,
                            isInternal);

                    var emailResponses
                        = emails?.Select(email => new EmailPackageActionResponse(email));
                    actionResponse = new()
                    {
                        Emails = emailResponses
                    };
                    valid = true;
                }
                catch (MinimumPermissionsNotGrantedException ex)
                {
                    logger.LogWarning(ex.Message);
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAccess);
                }
                catch (UnauthorisedException ex)
                {
                    logger.LogWarning(ex.Message);
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAccess);
                }
                break;
            case ActAction.Upload:
                if (act.Email?.File is null ||
                    act.Email.File.Length == 0)
                    break;
                var uploadResponse 
                    = await UploadEmail(
                        packageAction, 
                        userId, 
                        act.Email.File, 
                        permissionFlag,
                        actingPermissions, 
                        isInternal);
                if(!uploadResponse.Success)
                    return new(uploadResponse.StatusCode, 
                        error: uploadResponse.ResponseData?.Error);
                valid = true;
                break;
            case ActAction.AddAttachments:
                if (act.Email?.Attachments is null ||
                    act.Email.Attachments.Length == 0 ||
                    act.Email.EmailId is null)
                    break;

                var addAttachments 
                    = await AddAttachments(
                        act.Email.EmailId, 
                        act.Email.Attachments,
                        userId,
                        permissionFlag,
                        actingPermissions,
                        isInternal);
                if(!addAttachments.Success)
                    return new(addAttachments.StatusCode, 
                        error: addAttachments.ResponseData?.Error);
                valid = true;
                break; 
            case ActAction.Remove:
                if (act.Email?.EmailId is null) 
                    break;
                
                var removeEmail 
                    = await RemoveEmail(
                        act.Email.EmailId, userId,
                        permissionFlag,
                        actingPermissions,
                        isInternal);
                if(!removeEmail.Success)
                    return new(removeEmail.StatusCode, 
                        error: removeEmail.ResponseData?.Error);
                valid = true;
                break;
            case ActAction.None: // Unreachable
            default:
                return new(HttpStatusCode.BadRequest, 
                    error: new(eErrorCode.ValidationError, new[] 
                        { "Invalid action provided." }));
        }
        if(!valid)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, new[] 
                    { $"Invalid data for action ({act.Action}) provided." }));
        actionResponse ??= new(); // In the case an actionResponse was not created - it leaves the following response
                                  // {
                                  //    responseCode: 1,
                                  //    body: {}
                                  // } 
        return new(HttpStatusCode.OK, actionResponse);
    }

    private async Task<IList<EmailPackageActionDto>?> GetEmails(
        PackageActionDto packageAction,
        string query, 
        string userId,
        GlobalPermission flagPermissions, 
        PackageUserPermission? actingPermissions, 
        bool isInternal)
    {
        logger.LogInformation(nameof(GetEmails));

        if (isInternal)
        {
            logger.LogInformation("Searching for emails internally");
            return await context.EmailPackageActions
                .Include(x => x.Attachments)
                .Where(x => x.PackageActionId == packageAction.PackageActionId &&
                            x.EmailTsVector.Matches(query))
                .ToListAsync();
        }
        
        var flagPermissionsFound = flagPermissions.HasFlag(GlobalPermission.Administrator) || 
                                   flagPermissions.HasFlag(GlobalPermission.ReadEmails);
        
        // This is the case of a user with global permissions that are not overridden by additional permissions.
        if (flagPermissionsFound)
        {
            logger.LogInformation("Searching for emails using global permissions");
            return await context.EmailPackageActions
                .Include(x => x.Attachments)
                .Where(x => x.PackageActionId == packageAction.PackageActionId &&
                            x.EmailTsVector.Matches(query))
                .ToListAsync();
        }
            

        if (actingPermissions is null)
            throw new UnauthorisedException();

        var nonNullActingPermissions = (PackageUserPermission) actingPermissions;

        if (nonNullActingPermissions.HasFlag(PackageUserPermission.DefaultRead))
        {
            logger.LogInformation($"Searching for emails with default read permissions flag ({PackageUserPermission.DefaultRead})");
            return await context.EmailPackageActions
                .Include(x => x.Attachments)
                .Where(x => x.PackageActionId == packageAction.PackageActionId &&
                            x.EmailTsVector.Matches(query))
                .ToListAsync();
        }

        if (!nonNullActingPermissions.HasFlag(PackageUserPermission.ReadSelf)) 
            throw new MinimumPermissionsNotGrantedException();
        
        logger.LogInformation($"Searching for emails without read alt permission flag ({PackageUserPermission.ReadSelf})");
        return await context.EmailPackageActions
            .Include(x => x.Attachments)
            .Where(x => x.PackageActionId == packageAction.PackageActionId &&
                        x.EmailTsVector.Matches(query) && x.UploadedById == userId)
            .ToListAsync();
    } 

    private async Task<StatusContainer> UploadEmail(
        PackageActionDto packageAction, 
        string userId, 
        byte[] bytes,
        GlobalPermission flagPermissions, 
        PackageUserPermission? actingPermissions, 
        bool isInternal)
    {
        logger.LogInformation(nameof(UploadEmail));
        if (!isInternal)
        {
            var flagsPermissionFound = flagPermissions.HasFlag(GlobalPermission.Administrator) ||
                                       flagPermissions.HasFlag(GlobalPermission.UploadEmail);
            if (!flagsPermissionFound)
            {
                logger.LogInformation("No global permissions, checking action permissions");
                if(actingPermissions is null)
                    throw new UnauthorisedException();
                
                var nonNullActingPermissions = (PackageUserPermission) actingPermissions;
                if (!nonNullActingPermissions.HasFlag(PackageUserPermission.AddSelf))
                {
                    logger.LogInformation("The user does not have permission to upload emails");
                    return new(HttpStatusCode.Forbidden,
                        error: CodeMessageResponse.ForbiddenAction);
                }
            }
            else
                logger.LogInformation("Uploading email using global permissions");
        }
        else
            logger.LogInformation("Uploading email internally");
        try
        {
            await using var stream = new MemoryStream(bytes);

            var message = await MimeMessage.LoadAsync(stream);

            var attachments = new List<EmailAttachmentDto>();
            var emailDto = new EmailPackageActionDto
            {
                PackageActionId = packageAction.PackageActionId,
                Recipients = string.Join(", ", message.To.Mailboxes.Select(x => $"{x.Name} ({x.Address})")),
                Sender = string.Join(", ", message.From.Mailboxes.Select(x => $"{x.Name} ({x.Address})")),
                Subject = message.Subject,
                Body = message.TextBody,
                Attachments = attachments,
                UploadedById = userId
            };

            foreach (var attachmentEntity in message.Attachments)
            {
                if (attachmentEntity is not MimePart attachment)
                    continue;

                var filePath = Path.Combine(settings.CdnPath,
                    // Guid helps to randomise the file name and avoid overwriting existing files; and the file name furthers reduced naming clashes.
                    $"{Guid.NewGuid()}__{attachment.ContentDisposition.FileName}");
                await using var fileStream = File.Create(filePath);
                await attachment.Content.DecodeToAsync(fileStream);
                var emailAttachmentDto = new EmailAttachmentDto
                {
                    EmailPackageActionId = emailDto.EmailId,
                    Name = attachment.ContentDisposition.FileName,
                    FileType = attachment.ContentType.MimeType,
                    FilePath = fileStream.Name
                };
                attachments.Add(emailAttachmentDto);
            }

            await context.EmailPackageActions.AddAsync(emailDto);
            await context.SaveChangesAsync();

            return HttpStatusCode.OK;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to save email - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.BadRequest, new[]
                    { "Failed to save the email, please check the data again." }));
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Failed to save email - IOException");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.SaveFailed, new[]
                    { "Failed to save one or more attachments." }));
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Failed to save email - FormatException");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.InvalidFormat, new[]
                    { "The uploaded file was not in Email format (.eml)." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create package");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create the package, please try again later." }));
        }
    }

    private async Task<StatusContainer> AddAttachments(
        long? emailId,
        IEnumerable<EmailAttachment> attachments,
        string userId,
        GlobalPermission flagPermissions, 
        PackageUserPermission? actingPermissions, 
        bool isInternal)
    {
        logger.LogInformation(nameof(AddAttachments));
        
        try
        {
            var emailAction =
                await context.EmailPackageActions
                    .FirstOrDefaultAsync(x => x.EmailId == emailId);

            if (emailAction is null)
                return HttpStatusCode.NotFound;
            
            if (!isInternal)
            {
                var flagsPermissionFound = flagPermissions.HasFlag(GlobalPermission.Administrator) ||
                                           flagPermissions.HasFlag(GlobalPermission.UploadEmail);
                if (!flagsPermissionFound)
                {
                    if(actingPermissions is null)
                        throw new UnauthorisedException();
                
                    var nonNullActingPermissions = (PackageUserPermission) actingPermissions;
                    if (!nonNullActingPermissions.HasFlag(PackageUserPermission.UpdateSelf))
                        throw new MinimumPermissionsNotGrantedException();

                    // Checking whether the user is not the uploader of the email, however has permissions to update 
                    if (emailAction.UploadedById != userId &&
                        !nonNullActingPermissions.HasFlag(PackageUserPermission.UpdateAlt))
                        throw new UnauthorisedException(
                            "Attempting to modify another user's data without the required permissions.");
                }
            }

            foreach (var attachment in attachments)
            {
                var file = File.Create(Path.Combine(settings.CdnPath, $"{Guid.NewGuid()}__{attachment.Name}"));
                await file.WriteAsync(attachment.AttachmentBytes);
                var attachmentDto = new EmailAttachmentDto
                {
                    EmailPackageActionId = emailAction.EmailId,
                    Name = attachment.Name,
                    FileType = attachment.MimeType,
                    FilePath = file.Name
                };
                emailAction.Attachments.Add(attachmentDto);
            }
            await context.SaveChangesAsync();
            return HttpStatusCode.OK;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to save email - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.BadRequest, new[]
                    { "Failed to save the email, please check the data again." }));
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Failed to save email - IOException");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.SaveFailed, new[]
                    { "Failed to save one or more attachments." }));
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Failed to save email - FormatException");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.SaveFailed, new[]
                    { "The uploaded file was not in Email format (.eml)." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create package");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create the package, please try again later." }));
        }
    }

    private async Task<StatusContainer> RemoveEmail(
        long? emailId,
        string userId,
        GlobalPermission flagPermissions, 
        PackageUserPermission? actingPermissions, 
        bool isInternal)
    {
        logger.LogInformation(nameof(RemoveEmail));
        try
        {
            var emailAction =
                await context.EmailPackageActions
                    .Include(x => x.Attachments)
                   .FirstOrDefaultAsync(x => x.EmailId == emailId);

            if (emailAction is null)
                return HttpStatusCode.NotFound;
            
            if (!isInternal)
            {
                var flagsPermissionFound = flagPermissions.HasFlag(GlobalPermission.Administrator) ||
                                           flagPermissions.HasFlag(GlobalPermission.DeleteEmail);
                if (!flagsPermissionFound)
                {
                    logger.LogInformation("No global permissions, checking action permissions");
                    if(actingPermissions is null)
                        throw new UnauthorisedException();
                
                    var nonNullActingPermissions = (PackageUserPermission) actingPermissions;
                    if (!nonNullActingPermissions.HasFlag(PackageUserPermission.DeleteSelf))
                    {
                        logger.LogInformation("The user does not have permission to upload emails");
                        return new(HttpStatusCode.Forbidden,
                            error: CodeMessageResponse.ForbiddenAction);
                    }

                    // Checking whether the user is not the uploader of the email, however has permissions to update 
                    if (emailAction.UploadedById != userId &&
                        !nonNullActingPermissions.HasFlag(PackageUserPermission.DeleteAlt))
                        return new(HttpStatusCode.Forbidden, 
                            error: new(eErrorCode.Forbidden, new[] 
                                { "You do not have permission to remove the email." }));
                }
                else
                    logger.LogInformation("Removing email using global permissions");
            }
            else
                logger.LogInformation("Removing email using internal permissions");
            
            var attachments = emailAction.Attachments.ToList();
            foreach (var attachment in attachments)
            {
                try
                {
                    File.Delete(attachment.FilePath);
                    emailAction.Attachments.Remove(attachment);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete attachment");
                }
            }
            context.EmailPackageActions.Remove(emailAction);
            await context.SaveChangesAsync();
            return HttpStatusCode.OK;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to remove email - DbUpdateException");
            return new(HttpStatusCode.BadRequest,
                new(eErrorCode.BadRequest, new[]
                    { "Failed to remove the email, please ensure the email exists." }));
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Failed to remove email - IOException");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.SaveFailed, new[]
                    { "Failed to remove one or more attachments." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to email package");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to remove the email, please try again later." }));
        }
    }

    public async Task<StatusContainer<(Stream, string, string)>> DownloadAttachment(
        long attachmentId, 
        bool isInternal = false)
    {
        logger.LogInformation(nameof(DownloadAttachment));
        
        var claims = httpContextAccessor.HttpContext?.User;
    
        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        
        var attachment = await context.EmailAttachments
            .AsNoTracking()
            .Include(x => x.EmailPackageAction)
             .ThenInclude(x => x.PackageAction)
              .ThenInclude(x => x.Package)
               .ThenInclude(x => x.Teams)
                .ThenInclude(x => x.UserGroups)
                 .ThenInclude(x => x.Users)
            .Include(x => x.EmailPackageAction)
             .ThenInclude(x => x.PackageAction)
              .ThenInclude(x => x.Package)
               .ThenInclude(x => x.Teams)
                .ThenInclude(x => x.Users)
            .Include(x => x.EmailPackageAction)
             .ThenInclude(x => x.PackageAction)
              .ThenInclude(x => x.Package)
               .ThenInclude(x => x.Administrators)
            .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);
    
        if (attachment is null)
            return new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound, new[] 
                    { "The attachment was not found." }));

        if (isInternal)
        {
            logger.LogInformation("Allowing file download internally");
            return new(HttpStatusCode.OK,
                (File.OpenRead(attachment.FilePath), 
                    attachment.FileType, 
                    attachment.Name.Split("__")[^1]));
        }
            
        var validation = await ValidateUser(claims);
        if(!validation.Success || validation.ResponseData.Body is null)
            return new(validation.StatusCode, 
                error: validation.ResponseData.Error);
    
        var userId = validation.ResponseData.Body.UserId;
        var permissionFlag = validation.ResponseData.Body.PermissionFlag;
        
        var flagPermissionsFound = permissionFlag.HasFlag(GlobalPermission.Administrator) ||
                                   permissionFlag.HasFlag(GlobalPermission.ReadPackage);
        attachment.EmailPackageAction.PackageAction.UserInPackageAction(userId, out var actingPermissions);

        if (!flagPermissionsFound)
        {
            logger.LogInformation("No global permissions, checking action permissions");
            if(actingPermissions is null)
                throw new UnauthorisedException();
                
            var nonNullActingPermissions = (PackageUserPermission) actingPermissions;
            if (nonNullActingPermissions.HasFlag(PackageUserPermission.ReadSelf))
                return new(HttpStatusCode.OK,
                    (File.OpenRead(attachment.FilePath), 
                        attachment.FileType, 
                        attachment.Name.Split("__")[^1]));
            logger.LogInformation("User has no permission to download attachments");
            return new(HttpStatusCode.Forbidden,
                error: CodeMessageResponse.ForbiddenAction);
        }
        logger.LogInformation("Allowing file download using global permissions");
        return new(HttpStatusCode.OK,
            (File.OpenRead(attachment.FilePath), 
                attachment.FileType, 
                attachment.Name.Split("__")[^1]));
    }
}