using System.Net;
using System.Security.Claims;
using AppliedSoftware.Extensions;
using AppliedSoftware.Models.Validators;
using AppliedSoftware.Models.DTOs;
using AppliedSoftware.Models.Enums;
using AppliedSoftware.Models.Request;
using AppliedSoftware.Models.Response;
using AppliedSoftware.Models.Response.PackageActionsAct;
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
                DefaultAllowedPermissions = newTeam.DefaultAllowedPermissions ?? PackageActionPermission.None,
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
            Enum.TryParse<PackageActionType>(packageActionIdentifier, out var packageActionType);
        
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
        // NOTE: This does not check for permission overrides.
        var userInPackage = packageAction.UserInPackageAction(userId);
        
         // If the user does not have the global permission Administrator or ReadPackage
         if (flagPermissionsFound ||
             // Then we need to check if the user is a member of the requested package
             userInPackage) 
             return new(HttpStatusCode.OK, packageAction.RemoveNavigationProperties());
         
         logger.LogWarning($"User {userId} does not have the required permissions to read the package (package actions)");
         return new(HttpStatusCode.Forbidden, 
             error: CodeMessageResponse.ForbiddenAction);
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
            Enum.TryParse<PackageActionType>(packageActionIdentifier, out var packageActionType);
        
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

        act.Action = char.ToUpper(act.Action[0]) + act.Action[1..];
        var validActionType = Enum.TryParse<ActAction>(act.Action, out var actAction);
        // Validation on the action
        var messages = new List<string>();
        if(!validActionType || 
           actAction == ActAction.None)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, new[] 
                    { "Invalid action provided." }));

        var valid = false;

        ActionResponse? actionResponse = null;
        
        switch (actAction)
        {
            // Current Search internally redirects to ViewEmail
            case ActAction.Search:
            case ActAction.ViewEmail:
                if(string.IsNullOrWhiteSpace(act.Email?.SearchEmailContent))
                    break;
                var emails
                    = await GetEmails(packageAction, act.Email.SearchEmailContent);
                actionResponse = new()
                {
                    Emails = emails
                };

                valid = true;
                break;
            case ActAction.Upload:
                if (act.Email?.File is null ||
                    act.Email.File.Length == 0)
                    break;
                var response = await UploadEmail(packageAction, act.Email.File);
                if(!response.Success)
                    return new(response.StatusCode, 
                        error: response.Error);
                valid = true;
                break;
            case ActAction.AppendAttachment:
                if(act.Email?.AttachmentBytes is not null && 
                   act.Email.AttachmentBytes.Length > 0 && 
                   !string.IsNullOrWhiteSpace(act.Email.EmailIdentifier))
                    valid = true;
                break;
            case ActAction.Remove:
                if (!string.IsNullOrWhiteSpace(act.Email?.EmailIdentifier))
                    valid = true;
                break;
            case ActAction.None: // Unreachable
            default:
                throw new ArgumentOutOfRangeException(nameof(act.Action));
        }
        
        
        if(!valid)
            return new(HttpStatusCode.BadRequest, 
                error: new(eErrorCode.ValidationError, new[] 
                    { $"Invalid data for action ({act.Action}) provided." }));
        
        return new(HttpStatusCode.OK, actionResponse);
    }

    private async Task<IList<EmailPackageActionDto>> GetEmails(PackageActionDto packageAction, string query)
        => await context.EmailPackageActions
            .Where(x => x.PackageActionId == packageAction.PackageActionId &&
                        x.EmailTsVector.Matches(query))
            .ToListAsync();

    private async Task<StatusContainer> UploadEmail(
        PackageActionDto packageAction, 
        byte[] bytes)
    {
        try
        {
            await using var stream = new MemoryStream(bytes);

            var message = await MimeMessage.LoadAsync(stream);

            var emailDto = new EmailPackageActionDto
            {
                PackageActionId = packageAction.PackageActionId,
                Recipients = string.Join(", ", message.To.Mailboxes.Select(x => $"{x.Name} ({x.Address})")),
                Sender = string.Join(", ", message.From.Mailboxes.Select(x => $"{x.Name} ({x.Address})")),
                Subject = message.Subject,
                Body = message.TextBody
            };
            // TODO: is it possible to add the attachments via the Attachments navigation property without needing to set the email ID below?
            await context.EmailPackageActions.AddAsync(emailDto);
            await context.SaveChangesAsync();

            foreach (var attachmentEntity in message.Attachments)
            {
                if (attachmentEntity is not MimePart attachment)
                    continue;

                var fileNameParts = attachment.ContentDisposition.FileName.Split('.');
                var filePath = Path.Combine(settings.CdnPath,
                    $"{emailDto.EmailId}-{fileNameParts[0]}__{Guid.NewGuid()}.{fileNameParts[^1]}");

                await using var fileStream = File.Create(filePath);
                await attachment.Content.DecodeToAsync(fileStream);
                var emailAttachmentDto = new EmailAttachmentDto
                {
                    EmailPackageActionId = emailDto.EmailId,
                    Name = attachment.ContentDisposition.FileName,
                    FileType = attachment.ContentType.MimeType,
                    FilePath = filePath
                };
                await context.EmailAttachments.AddAsync(emailAttachmentDto);
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
                    { "The uploaded file was not an Email format (.eml)." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create package");
            return new(HttpStatusCode.InternalServerError,
                new(eErrorCode.ServiceUnavailable, new[]
                    { "Failed to create the package, please try again later." }));
        }
    }
}