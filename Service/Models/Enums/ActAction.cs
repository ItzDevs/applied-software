namespace AppliedSoftware.Models.Enums;

public enum ActAction
{
    /// <summary>
    /// Base case.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Only available in an <see cref="PackageActionType.Search"/> package action.
    /// </summary>
    Search = 1,
    
    /// <summary>
    /// Only available in an <see cref="PackageActionType.Email"/> package action.
    /// </summary>
    ViewEmail = 2,
    
    /// <summary>
    /// Only available in an <see cref="PackageActionType.Email"/> package action.
    /// </summary>
    Upload = 3,
    
    /// <summary>
    /// Only available in an <see cref="PackageActionType.Email"/> package action.
    /// </summary>
    AddAttachment = 4,
    
    /// <summary>
    /// Only available in an <see cref="PackageActionType.Email"/> package action.
    /// </summary>
    Remove = 5
}