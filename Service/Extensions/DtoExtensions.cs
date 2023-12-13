using AppliedSoftware.Models.DTOs;
using FirebaseAdmin.Auth;

namespace AppliedSoftware.Extensions;

public static class DtoExtensions
{
    public static UserDto ToUserDto(this ExportedUserRecord userRecord) 
        => new(userRecord);
    
    public static IEnumerable<UserDto> ToUserDtos(this IEnumerable<ExportedUserRecord> userRecords) 
        => userRecords.Select(userRecord => userRecord.ToUserDto());
}