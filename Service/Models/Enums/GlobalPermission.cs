namespace AppliedSoftware.Models.Enums;

[Flags]
public enum GlobalPermission
{
    None = 0,
    
    Administrator = 1,
    
    CreateTeam = 2,
    
    DeleteTeam = 4,
    
    ModifyTeam = 8,
    
    ReadTeam = 16,
    
    CreateUser = 32,
    
    DeleteUser = 64,
    
    ModifyUser = 128,
    
    AddUserToTeam = 256,
    
    RemoveUserFromTeam = 512,
    
    CreatePackage = 1024,
    
    DeletePackage = 2048,
    
    ModifyPackage = 4096,
    
    ReadPackage = 8192,
    
    CreateUserGroup = 16384,
    
    DeleteUserGroup = 32768,
    
    ModifyUserGroup = 65536,
    
    ReadUserGroup = 131072,
    
    AddUserToGroup = 262144,
    
    RemoveUserFromGroup = 524288,
    
    ReadEmails = 1048576,
    
    UploadEmail = 2097152,
    
    DeleteEmail = 4194304,
    
    AddAttachment = 8388608,
    
    Remove = 16777216,
}