using AppliedSoftware.Models.DTOs;
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
        return package.Administrators.Any(x => x.Uid == userId) || 
               package.Teams.Any(team => team.Users.Any(member => member.Uid == userId)) ||
               package.Teams.Any(team => team.UserGroups.Any(ug => 
                   ug.Users.Any(member => member.Uid == userId)));
    }

    public static bool UserInPackageAction(this PackageActionDto packageAction, string userId)
    {
        return packageAction.UserPermissionOverrides.Any(x => x.UserId == userId) || 
               packageAction.Package.UserInPackage(userId);
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
            packageActionDto.TeamPermissionOverrides = null!;
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