namespace AppliedSoftware.Models.Enums;

[Flags]
public enum PackageActionPermission
{
    /// <summary>
    /// No Permissions / Default.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Gives user default access to read information their own uploaded information within a package action.
    /// </summary>
    ReadSelf = 2,
    
    /// <summary>
    /// Gives user default access to delete information uploaded by themselves within a package action.
    /// </summary>
    DeleteSelf = 4,
    
    /// <summary>
    /// Gives user default access to update information uploaded by themselves within a package action.
    /// </summary>
    UpdateSelf = 8,
    
    
    /// <summary>
    /// Gives user default access to upload information within a package action.
    /// </summary>
    AddSelf = 16,
    
    /// <summary>
    /// Gives user default access to do any self-related permissions information within a package action.
    /// </summary>
    
    /// <summary>
    /// Gives a user access to read information uploaded by other users within a package action.
    /// </summary>
    ReadAlt = 32,
    
    /// <summary>
    /// <see cref="DefaultRead"/> is generally considered to be the minimum requirement to read data for packages.
    /// </summary>
    DefaultRead = ReadSelf | ReadAlt,
    
    /// <summary>
    /// View actions allow a user to see package actions within a package,
    /// when defined at a Package/Team level the permission allows all package actions to be viewed.
    /// </summary>
    ViewAction = 1024,

    
    //SelfPermissions = DefaultRead | UpdateSelf | DeleteSelf | AddSelf,
    
    // ---- BELOW THIS LINE ARE GENERALLY CONSIDERED ADMINISTRATIVE PERMISSIONS ---- \\
    
    /// <summary>
    /// Allows a user to delete information uploaded by other users (themself included) within a package action.
    /// </summary>
    DeleteAlt = 64,
    
    /// <summary>
    /// Allows a user to upload information acting on another users behalf within a package action.
    /// </summary>
    AddAlt = 256,
    
    UpdateAlt = 512,
    
    
    CreateAction = 2048,
    
    UpdateAction = 4096,
    
    DeleteAction = 8192,
    
    Administrator =  ReadSelf     | 
                     DeleteSelf   | 
                     UpdateSelf   | 
                     AddSelf      | 
                     DeleteAlt    | 
                     AddAlt       | 
                     UpdateAlt    | 
                     ViewAction   | 
                     CreateAction | 
                     UpdateAction | 
                     DeleteAction
}