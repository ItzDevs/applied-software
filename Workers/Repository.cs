using System.Net;
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

    public async Task<StatusContainer> CreateTeam(
        CreateTeam newTeam,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(CreateTeam)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
        {
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);
        }

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

    public async Task<StatusContainer<IEnumerable<TeamDto>>> GetTeams(
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(GetTeams)}");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);

        List<TeamDto> userTeams = [];
        List<TeamDto> readTeams = [];
        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
            {
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            }
            var userInTeam 
                = await GetUser(
                    userId);
            if(!userInTeam.Success)
                return new(userInTeam.StatusCode, 
                    error: userInTeam.ResponseData.Error);
            var permissionFlag = validation.ResponseData.Body;
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ReadTeam)))
            {   // TODO: WOrk on the message.
                logger.LogWarning($"User {userId} does not have the required permissions to read team information");
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
        
        List<TeamDto> grouped = [..userTeams, ..readTeams];
        grouped = grouped.DistinctBy(x => x.TeamId).ToList();
        return grouped.Count > 0 ? 
            new(HttpStatusCode.OK, grouped) : 
            new(HttpStatusCode.Forbidden, 
                error: new(eErrorCode.Forbidden, new[] 
                    { "You do not have access to any teams" }));
    }
    
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

        bool isUsingId = long.TryParse(teamIdentifier, out var teamId);
        
        
        if (!isInternal)
        {
            var userId = authenticationService.ExtractUserId(claims);
            var validation = await ValidateUser(userId);
            if (!validation.Success)
            {
                return new(validation.StatusCode, 
                    error: validation.ResponseData.Error);
            }

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
            if (!(permissionFlag.HasFlag(GlobalPermission.Administrator) || 
                  permissionFlag.HasFlag(GlobalPermission.ReadTeam)) || 
                !(teamsAsIds?.Contains(teamId) == false || 
                 teamsAsNames?.Contains(teamIdentifier) == false))
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

        team.Deleted = true;
        team.UpdatedAtUtc = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return HttpStatusCode.OK;
    }

    public async Task<StatusContainer> CreateUserGroup(
        CreateUserGroup newUserGroup,
        bool isInternal = false)
    {
        logger.LogInformation($"{nameof(CreateUserGroup)} (isInternal={isInternal})");
        var claims = httpContextAccessor.HttpContext?.User;

        // Authentication
        if (claims is null && !isInternal)
        {
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);
        }

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
                Name = newUserGroup.Name,
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
}

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
    /// Create a new user group if the required permissions are granted, or return an error that can be returned
    /// </summary>
    /// <param name="newUserGroup"></param>
    /// <param name="isInternal"></param>
    /// <returns></returns>
    Task<StatusContainer> CreateUserGroup(
        CreateUserGroup newUserGroup,
        bool isInternal = false);
}