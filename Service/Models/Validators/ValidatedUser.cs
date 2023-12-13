using AppliedSoftware.Models.Enums;

namespace AppliedSoftware.Models.Validators;

public record ValidatedUser(string UserId, GlobalPermission PermissionFlag);