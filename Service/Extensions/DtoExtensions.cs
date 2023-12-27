using AppliedSoftware.Models.DTOs;
using AppliedSoftware.Models.Enums;
using AppliedSoftware.Models.Response.PackageActions;
using FirebaseAdmin.Auth;

namespace AppliedSoftware.Extensions;

public static class DtoExtensions
{
    /// <summary>
    /// Converts a <see cref="ExportedUserRecord"/> to a <see cref="UserDto"/>.
    /// </summary>
    /// <param name="userRecord"></param>
    /// <returns></returns>
    public static UserDto ToUserDto(this ExportedUserRecord userRecord)
        => new(userRecord);

    /// <summary>
    /// Converts an enumerable of <see cref="ExportedUserRecord"/> to a <see cref="UserDto"/>.
    /// </summary>
    /// <param name="userRecords"></param>
    /// <returns></returns>
    public static IEnumerable<UserDto> ToUserDtos(this IEnumerable<ExportedUserRecord> userRecords)
        => userRecords.Select(userRecord => userRecord.ToUserDto());
    
    /// <summary>
    /// Gets the packages from a user through all navigational paths.
    /// </summary>
    /// <param name="userDto"></param>
    /// <returns></returns>
    public static IEnumerable<PackageDto> GetPackages(this UserDto userDto)
    {
        var packages = userDto.Teams.Select(team => team.Package).ToList();

        packages.AddRange(userDto.PackageAdministrator);
        packages.AddRange(userDto.UserPermissionOverrides.Select(permissionOverride => permissionOverride.Package));
        return packages;
    }

    public static bool UserInPackage(this PackageDto package, string userId)
    {
        var isInPackage = package.Administrators.Any(x => x.Uid == userId) ||
                           package.Teams.Any(team => team.Users.Any(member => member.Uid == userId)) ||
                           package.Teams.Any(team => team.UserGroups.Any(ug =>
                               ug.Users.Any(member => member.Uid == userId)));
        return isInPackage;
    }

    private static PackageUserPermission ProcessUserGroupPermissions(
        PackageDto package,
        PackageUserPermission basePermissions,
        string userId)
    {
        var grantedUserGroupOverrides = new List<PackageUserPermission>();
        var disallowedUserGroupOverrides = new List<PackageUserPermission>();

        // Flatten all the user groups and filter out any group that the user is not a member of.
        var userGroupsFlat = package.Teams
            .SelectMany(team => team.UserGroups)
            .Where(ug => ug.Users
                .Any(member => member.Uid == userId));

        foreach (var userGroup in userGroupsFlat)
        {
            if(userGroup.AllowedPermissions is not null)
                grantedUserGroupOverrides.Add((PackageUserPermission) userGroup.AllowedPermissions);
            
            if(userGroup.DisallowedPermissions is not null)
                disallowedUserGroupOverrides.Add((PackageUserPermission) userGroup.DisallowedPermissions);
        }


        foreach (var grantedPermission in grantedUserGroupOverrides)
        {
            basePermissions |= grantedPermission;
        }
        
        foreach (var disallowedPermission in disallowedUserGroupOverrides)
        {
            basePermissions &= ~disallowedPermission;
        }
        
        return basePermissions;
    }

    public static PackageUserPermission GenerateActingPermissions(PackageDto package, string userId)
    {
        var teamsWithUser = package.Teams.Where(team => team.Users.Any(member => member.Uid == userId)).ToList();

        TeamDto? highestPermissionTeam = null;
        var highestGrantedPermissionTeam = -1;
        foreach (var team in teamsWithUser)
        {
            // Special case; this will ALWAYS be the highest denied permission.
            if (team.DefaultAllowedPermissions.HasFlag(PackageUserPermission.Administrator))
            {
                highestPermissionTeam = team;
                break;
            }

            var grantedPermissionCount 
                = Enum.GetValues(typeof(PackageUserPermission))
                    .Cast<PackageUserPermission>()
                    .Count(e => team.DefaultAllowedPermissions.HasFlag(e));

            if (grantedPermissionCount < highestGrantedPermissionTeam) 
                continue;
            
            highestPermissionTeam = team;
            highestGrantedPermissionTeam = grantedPermissionCount;
        }

        if (highestPermissionTeam is null)
            throw new Exception("No teams had permissions");

        var basePermissions = ProcessUserGroupPermissions(package, highestPermissionTeam.DefaultAllowedPermissions, userId);
        
        
        if (package.Administrators.Any(x => x.Uid == userId))
            basePermissions |= PackageUserPermission.Administrator;
        return basePermissions;
    }

    public static PackageUserPermission GenerateActingPermissions(PackageActionDto packageAction, string userId)
    {
        // In this case, we do not need to process anything as the user, no matter what, is an administrator.
        if (packageAction.Package.Administrators.Any(x => x.Uid == userId))
        {
            return PackageUserPermission.Administrator;
        }
        
        var basePermissions = GenerateActingPermissions(packageAction.Package, userId);
        
        var grantedUserGroupOverrides = new List<PackageUserPermission>();
        var disallowedUserGroupOverrides = new List<PackageUserPermission>();

        foreach (var packageActionPermissionOverride in packageAction.UserGroupPermissionOverrides)
        {
            grantedUserGroupOverrides.Add(packageActionPermissionOverride.AllowedPermissions);
            
            disallowedUserGroupOverrides.Add(packageActionPermissionOverride.DisallowedPermissions);
        }
        
        foreach (var grantedPermission in grantedUserGroupOverrides)
        {
            basePermissions |= grantedPermission;
        }
        
        foreach (var disallowedPermission in disallowedUserGroupOverrides)
        {
            basePermissions &= ~disallowedPermission;
        }
        
        // Now user group overrides have been processed, the individual user overrides need to be processed - which take 
        // precedence.
        
        // There should only be one user override per package action.
        var individualUserOverride = packageAction.UserPermissionOverrides
            .FirstOrDefault(x => x.UserId == userId);

        if (individualUserOverride is null)
            return basePermissions;
        
        basePermissions |= individualUserOverride.AllowedPermissions;
        basePermissions &= ~individualUserOverride.DisallowedPermissions;
        
        return basePermissions;
    }

    public static bool UserInPackageAction(
        this PackageActionDto packageAction, 
        string userId, 
        out PackageUserPermission? actingPermissions)
    {
        // out parameters cannot have default values, so this is its default value.
        actingPermissions = null;

        // This confirms that we should continue with working out the actingPermissions value.
        var userInPackage = packageAction.UserPermissionOverrides.Any(x => x.UserId == userId) ||
                            packageAction.Package.UserInPackage(userId);

        if (!userInPackage) 
            return userInPackage;
        
        actingPermissions = GenerateActingPermissions(packageAction, userId);
        return userInPackage;
    }

    public static EmailAttachmentResponse ToEmailAttachmentResponse(this EmailAttachmentDto attachment)
    {
        return new(attachment);
    }    
    
    public static IEnumerable<EmailAttachmentResponse> ToEmailAttachmentResponse(
        this IEnumerable<EmailAttachmentDto> emailAttachments)
    {
        return emailAttachments.Select(emailAttachment => emailAttachment.ToEmailAttachmentResponse());
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="emailAttachmentDto"></param>
    /// <param name="emailPackageActionDto"></param>
    /// <returns></returns>
    public static EmailAttachmentDto RemoveNavigationProperties(
        this EmailAttachmentDto emailAttachmentDto, 
        bool emailPackageActionDto = true)
    {
        if(emailPackageActionDto)
            emailAttachmentDto.EmailPackageAction = null!;
        return emailAttachmentDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="emailPackageActionDto"></param>
    /// <param name="packageAction"></param>
    /// <param name="emailAttachments"></param>
    /// <returns></returns>
    public static EmailPackageActionDto RemoveNavigationProperties(
        this EmailPackageActionDto emailPackageActionDto,
        bool packageAction = true,
        bool emailAttachments = true)
    {
        if(packageAction)
            emailPackageActionDto.PackageAction = null!;
        if(emailAttachments)
            emailPackageActionDto.Attachments = null!;
        return emailPackageActionDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="globalPermissionDto"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public static GlobalPermissionDto RemoveNavigationProperties(
        this GlobalPermissionDto globalPermissionDto,
        bool user = true)
    {
        if(user)
            globalPermissionDto.User = null!;
        return globalPermissionDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="packageActionDto"></param>
    /// <param name="package"></param>
    /// <param name="emails"></param>
    /// <param name="userPermissionOverrides"></param>
    /// <param name="teamPermissionOverrides"></param>
    /// <returns></returns>
    public static PackageActionDto RemoveNavigationProperties(
        this PackageActionDto packageActionDto,
        bool package = true,
        bool emails = true,
        bool userPermissionOverrides = true,
        bool teamPermissionOverrides = true)
    {
        if(package)
            packageActionDto.Package = null!;
        if(emails)
            packageActionDto.Emails = null!;
        if(userPermissionOverrides)
            packageActionDto.UserPermissionOverrides = null!;
        if(teamPermissionOverrides)
            packageActionDto.UserGroupPermissionOverrides = null!;
        return packageActionDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="packageDto"></param>
    /// <param name="teams"></param>
    /// <param name="administrators"></param>
    /// <param name="actions"></param>
    /// <returns></returns>
    public static PackageDto RemoveNavigationProperties(
        this PackageDto packageDto,
        bool teams = true,
        bool administrators = true,
        bool actions = true)
    {
        if(teams)
            packageDto.Teams = null!;
        if(administrators)
            packageDto.Administrators = null!;
        if(actions)
            packageDto.Actions = null!;
        return packageDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="teamDto"></param>
    /// <param name="package"></param>
    /// <param name="users"></param>
    /// <param name="userGroups"></param>
    /// <returns></returns>
    public static TeamDto RemoveNavigationProperties(
        this TeamDto teamDto,
        bool package = true,
        bool users = true,
        bool userGroups = true)
    {
        if(package)
            teamDto.Package = null!;
        if(users) 
            teamDto.Users = null!;
        if(userGroups)
            teamDto.UserGroups = null!;
        return teamDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="userDto"></param>
    /// <param name="packageAdministrator"></param>
    /// <param name="teams"></param>
    /// <param name="userGroups"></param>
    /// <param name="userPermissionOverrides"></param>
    /// <returns></returns>
    public static UserDto RemoveNavigationProperties(
        this UserDto userDto,
        bool packageAdministrator = true,
        bool teams = true,
        bool userGroups = true,
        bool userPermissionOverrides = true)
    {
        if(packageAdministrator)
            userDto.PackageAdministrator = null!;
        if(teams)
            userDto.Teams = null!;
        if(userGroups)
            userDto.UserGroups = null!;
        if(userPermissionOverrides)
            userDto.UserPermissionOverrides = null!;
        return userDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="userGroupDto"></param>
    /// <param name="users"></param>
    /// <returns></returns>
    public static UserGroupDto RemoveNavigationProperties(
        this UserGroupDto userGroupDto,
        bool users = true)
    {
        if(users)
            userGroupDto.Users = null!;
        return userGroupDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="userGroupPermissionOverrideDto"></param>
    /// <param name="userGroups"></param>
    /// <param name="package"></param>
    /// <param name="packageActions"></param>
    /// <returns></returns>
    public static UserGroupPermissionOverrideDto RemoveNavigationProperties(
        this UserGroupPermissionOverrideDto userGroupPermissionOverrideDto,
        bool userGroups = true,
        bool package = true,
        bool packageActions = true)
    {
        if(userGroups)
            userGroupPermissionOverrideDto.UserGroup = null!;
        if(package)
            userGroupPermissionOverrideDto.Package = null!;
        if(packageActions)
            userGroupPermissionOverrideDto.PackageAction = null;
        return userGroupPermissionOverrideDto;
    }

    /// <summary>
    /// Removes navigational properties. </summary>
    /// <param name="userPermissionOverrideDto"></param>
    /// <param name="user"></param>
    /// <param name="package"></param>
    /// <param name="packageActions"></param>
    /// <returns></returns>
    public static UserPermissionOverrideDto RemoveNavigationProperties(
        this UserPermissionOverrideDto userPermissionOverrideDto,
        bool user = true,
        bool package = true,
        bool packageActions = true) 
    {
        if(user)
            userPermissionOverrideDto.User = null!;
        if(package)
            userPermissionOverrideDto.Package = null!;
        if(packageActions)
            userPermissionOverrideDto.PackageAction = null;
        return userPermissionOverrideDto;
    }
}