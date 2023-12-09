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
    IHttpContextAccessor httpContextAccessor) : IRepository
{
    public async Task<StatusContainer<GlobalPermissionDto>> GetGlobalPermissionsForUser(
        string userId, 
        GlobalPermission permissionFlags)
    {
        var user = await context.GlobalPermissions
            .FirstOrDefaultAsync(x => x.UserId == userId && 
                                      x.GrantedGlobalPermission.HasFlag(permissionFlags));

        return user is not null
            ? new(HttpStatusCode.OK, user)
            : new(HttpStatusCode.NotFound,
                error: new(eErrorCode.NotFound,
                    new[] { "No user global permissions found." }));
    }

    public async Task<StatusContainer> CreateTeam(
        CreateTeam newTeam,
        bool isInternal = false)
    {
        var claims = httpContextAccessor.HttpContext?.User;

        if (claims is null && !isInternal)
        {
            return new(HttpStatusCode.Unauthorized,
                CodeMessageResponse.Unauthorised);
        }
        
        var userId = authenticationService.ExtractUserId(claims);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new(HttpStatusCode.Unauthorized, 
                CodeMessageResponse.Unauthorised);
        }

        var getUserGlobalPermissions
            = await GetGlobalPermissionsForUser(
                userId,
                GlobalPermission.Administrator |
                GlobalPermission.CreateTeam);
        //TODO: Implement the Create Team method.
        throw new NotImplementedException();
    }
    
    
}

public interface IRepository
{
    
}