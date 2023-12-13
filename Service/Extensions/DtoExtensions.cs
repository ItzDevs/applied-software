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

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="emailAttachmentDto"></param>
    /// <returns></returns>
    public static EmailAttachmentDto RemoveNavigationProperties(this EmailAttachmentDto emailAttachmentDto)
    {
        emailAttachmentDto.EmailPackageActionDto = null!;
        return emailAttachmentDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="emailPackageActionDto"></param>
    /// <returns></returns>
    public static EmailPackageActionDto RemoveNavigationProperties(this EmailPackageActionDto emailPackageActionDto)
    {
        emailPackageActionDto.PackageAction = null!;
        emailPackageActionDto.Attachments = null!;
        return emailPackageActionDto;
    }
    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="globalPermissionDto"></param>
    /// <returns></returns>
    public static GlobalPermissionDto RemoveNavigationProperties(this GlobalPermissionDto globalPermissionDto)
    {
        globalPermissionDto.User = null!;
        return globalPermissionDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="packageActionDto"></param>
    /// <returns></returns>
    public static PackageActionDto RemoveNavigationProperties(this PackageActionDto packageActionDto)
    {
        packageActionDto.Package = null!;
        packageActionDto.Emails = null!;
        packageActionDto.UserPermissionOverrides = null!;
        packageActionDto.TeamPermissionOverrides = null!;
        return packageActionDto;
    }
    
    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="packageDto"></param>
    /// <returns></returns>
    public static PackageDto RemoveNavigationProperties(this PackageDto packageDto)
    {
        packageDto.Teams = null!;
        packageDto.Administrators = null!;
        packageDto.Actions = null!;
        return packageDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="teamDto"></param>
    /// <returns></returns>
    public static TeamDto RemoveNavigationProperties(this TeamDto teamDto)
    {
        teamDto.Package = null!;
        teamDto.Users = null!;
        teamDto.UserGroups = null!;
        return teamDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="userDto"></param>
    /// <returns></returns>
    public static UserDto RemoveNavigationProperties(this UserDto userDto)
    {
        userDto.PackageAdministrator = null!;
        userDto.Teams = null!;
        userDto.UserGroups = null!;
        userDto.UserPermissionOverrides = null!;
        return userDto;
    }

    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="userGroupDto"></param>
    /// <returns></returns>
    public static UserGroupDto RemoveNavigationProperties(this UserGroupDto userGroupDto)
    {
        userGroupDto.Users = null!;
        return userGroupDto;
    }
    
    /// <summary>
    /// Removes navigational properties.
    /// </summary>
    /// <param name="userGroupPermissionOverrideDto"></param>
    /// <returns></returns>
    public static UserGroupPermissionOverrideDto RemoveNavigationProperties(this UserGroupPermissionOverrideDto userGroupPermissionOverrideDto)
    {
        userGroupPermissionOverrideDto.UserGroup = null!;
        userGroupPermissionOverrideDto.Package = null!;
        userGroupPermissionOverrideDto.PackageAction = null;
        return userGroupPermissionOverrideDto;
    }

    /// <summary>
    /// Removes navigational properties. </summary>
    /// <param name="userPermissionOverrideDto"></param>
    /// <returns></returns>
    public static UserPermissionOverrideDto RemoveNavigationProperties(this UserPermissionOverrideDto userPermissionOverrideDto)
    {
        userPermissionOverrideDto.User = null!;
        userPermissionOverrideDto.Package = null!;
        userPermissionOverrideDto.PackageAction = null;
        return userPermissionOverrideDto;
    }

}