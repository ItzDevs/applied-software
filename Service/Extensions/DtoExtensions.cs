using AppliedSoftware.Models.DTOs;
using FirebaseAdmin.Auth;

namespace AppliedSoftware.Extensions;

public static class DtoExtensions
{
    public static UserDto ToUserDto(this ExportedUserRecord userRecord) 
        => new(userRecord);
    
    public static IEnumerable<UserDto> ToUserDtos(this IEnumerable<ExportedUserRecord> userRecords) 
        => userRecords.Select(userRecord => userRecord.ToUserDto());

    public static UserDto RemoveCollections(this UserDto userDto)
    {
        userDto.PackageAdministrator = null!;
        userDto.Teams = null!;
        userDto.UserGroups = null!;
        userDto.UserPermissionOverrides = null!;
        return userDto;
    }

    public static UserGroupDto RemoveCollections(this UserGroupDto userGroupDto)
    {
        userGroupDto.Users = null!;
        return userGroupDto;
    }

    public static PackageDto RemoveCollections(this PackageDto packageDto)
    {
        packageDto.Teams = null!;
        packageDto.Administrators = null!;
        packageDto.Actions = null!;
        return packageDto;
    }
}