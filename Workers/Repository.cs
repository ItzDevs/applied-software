using System.Net;
using System.Security.Claims;
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
    public async Task<StatusContainer<GlobalPermissionDto>> GetGlobalPermissionsForUser(
        string userId)
    {
        var user = await context.GlobalPermissions
            .FirstOrDefaultAsync(x => x.UserId == userId);

        return user is not null
            ? new(HttpStatusCode.OK, user)
            : new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound,
                    new[] { "No user global permissions found." }));
    }

    private async Task<StatusContainer<GlobalPermission>> ValidateUser(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new(HttpStatusCode.Unauthorized,
                error: CodeMessageResponse.Unauthorised);
        }

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
        logger.LogInformation($"{nameof(CreateTeam)} (isInternal: {isInternal})");
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
            {
                return new(validation.StatusCode, 
                    validation.ResponseData.Error);
            }

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
                DefaultAllowedPermissions = newTeam.DefaultAllowedPermissions,
                DefaultDisallowedPermissions = newTeam.DefaultDisallowedPermissions,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            context.Teams.Add(team);
            await context.SaveChangesAsync();

            return new(HttpStatusCode.Created);
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
    Task<StatusContainer<GlobalPermissionDto>> GetGlobalPermissionsForUser(
        string userId);

    Task<StatusContainer> CreateTeam(
        CreateTeam newTeam,
        bool isInternal = false);
}