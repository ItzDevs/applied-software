using AppliedSoftware.Models.Enums;
using AppliedSoftware.Models.Request;
using AppliedSoftware.Models.Response;
using AppliedSoftwareTests.Mock;

namespace AppliedSoftwareTests;

public class ValidDataTests
{
    private MockRepository _validMocks = new MockRepository(
        GlobalPermission.Administrator,
        PackageActionPermission.Administrator,
        mockUserInX: true);
    
    
    #region Using highest permissions.
    [Theory]
    [InlineData("123456")]
    public async Task GlobalGetGlobalPermissionsForUser(string userId)
    {
        var result = await _validMocks.GetGlobalPermissionsForUser(userId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("123456")]
    public async Task GlobalUser(string userId)
    {
        var result = await _validMocks.GetUser(userId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    // A valid team, which also conflict for the UpdateTeam 
    public static IEnumerable<object[]> __CreateTeamNoConflict()
    {
        yield return new object[] { new CreateTeam
        {
            Name = "Test Team",
            Description = "This is a test team",
            DefaultAllowedPermissions = PackageActionPermission.Administrator,
            BelongsToPackageId = null
        }};
    }
    [Theory]
    [MemberData(nameof(__CreateTeamNoConflict))]
    public async Task GlobalCreateTeam(CreateTeam createTeam)
    {
        var result = await _validMocks.CreateTeam(createTeam);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Fact]
    public async Task GlobalGetTeams()
    {
        var result = await _validMocks.GetTeams();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalGetTeamId(string teamId)
    {
        var result = await _validMocks.GetTeam(teamId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalGetTeamName(string teamName)
    {
        var result = await _validMocks.GetTeam(teamName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateTeamNoConflict))]
    public async Task GlobalUpdateTeamById(CreateTeam updateTeam)
    {
        const string teamId = "1";
        var result = await _validMocks.UpdateTeam(teamId, updateTeam);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateTeamNoConflict))]
    public async Task GlobalUpdateTeamByName(CreateTeam updateTeam)
    {
        const string teamName = "one";
        var result = await _validMocks.UpdateTeam(teamName, updateTeam);
        
        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalDeleteTeamById(string teamId)
    {
        var result = await _validMocks.DeleteTeam(teamId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one")]
    public async Task GlobalDeleteTeamByName(string teamName)
    {
        var result = await _validMocks.DeleteTeam(teamName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "123456,123")]
    public async Task GlobalAddUsersToTeamById(string teamId, string userIds)
    {
        var result = await _validMocks.AddUsersToTeam(teamId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one", "two,one")]
    public async Task GlobalAddUsersToTeamByName(string teamId, string userIds)
    {
        var result = await _validMocks.AddUsersToTeam(teamId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "123456,one")]
    public async Task GlobalRemoveUsersFromTeamById(string teamId, string userIds)
    {
        var result = await _validMocks.RemoveUsersFromTeam(teamId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "123456,one")]
    public async Task GlobalRemoveUsersFromTeamByName(string teamId, string userIds)
    {
        var result = await _validMocks.RemoveUsersFromTeam(teamId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    public static IEnumerable<object[]> __CreateUserGroupNoConflict()
    {
        yield return new object[] { new CreateUserGroup()
        {
            Name = "Test User Group",
            Description = "This is a test user group",
            AllowedPermissions = PackageActionPermission.Administrator,
            DisallowedPermissions = PackageActionPermission.UpdateAction,
            TeamId = 1
        }};
    }
    [Theory]
    [MemberData(nameof(__CreateUserGroupNoConflict))]
    public async Task GlobalCreateUserGroup(CreateUserGroup createUserGroup)
    {
        var result = await _validMocks.CreateUserGroup(createUserGroup);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Fact]
    public async Task GlobalGetUserGroups()
    {
        var result = await _validMocks.GetUserGroups();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalGetUserGroupById(string userGroupId)
    {
        var result = await _validMocks.GetUserGroup(userGroupId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalGetUserGroupName(string userGroupName)
    {
        var result = await _validMocks.GetUserGroup(userGroupName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateUserGroupNoConflict))]
    public async Task GlobalUpdateUserGroupById(CreateUserGroup updateUserGroup)
    {
        const string userGroupId = "1";
        var result = await _validMocks.UpdateUserGroup(userGroupId, updateUserGroup);

        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateUserGroupNoConflict))]
    public async Task GlobalUpdateUserGroupByName(CreateUserGroup updateUserGroup)
    {
        const string userGroupName = "one";
        var result = await _validMocks.UpdateUserGroup(userGroupName, updateUserGroup);
        
        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalDeleteUserGroupById(string userGroupId)
    {
        var result = await _validMocks.DeleteUserGroup(userGroupId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalDeleteUserGroupByName(string userGroupName)
    {
        var result = await _validMocks.DeleteUserGroup(userGroupName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "two,one")]
    public async Task GlobalAddUsersToUserGroupById(string userGroupId, string userIds)
    {
        var result = await _validMocks.AddUsersToUserGroup(userGroupId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "two,one")]
    public async Task GlobalAddUsersToUserGroupByName(string userGroupId, string userIds)
    {
        var result = await _validMocks.AddUsersToUserGroup(userGroupId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "two,one")]
    public async Task GlobalRemoveUsersFromUserGroupById(string userGroupId, string userIds)
    {
        var result = await _validMocks.RemoveUsersFromUserGroup(userGroupId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "two,one")]
    public async Task GlobalRemoveUsersFromUserGroupByName(string userGroupId, string userIds)
    {
        var result = await _validMocks.RemoveUsersFromUserGroup(userGroupId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    public static IEnumerable<object[]> __CreatePackageNoConflict()
    {
        yield return new object[] { new CreatePackage()
        {
            Name = "Test User Group",
            Description = "This is a test user group"
        }};
    }

    [Theory]
    [MemberData(nameof(__CreatePackageNoConflict))]
    public async Task GlobalCreatePackage(CreatePackage createPackage)
    {
        var result = await _validMocks.CreatePackage(createPackage);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Fact]
    public async Task GlobalGetPackages()
    {
        var result = await _validMocks.GetPackages();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalGetPackageById(string packageId)
    {
        var result = await _validMocks.GetPackage(packageId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalGetPackageByName(string packageName)
    {
        var result = await _validMocks.GetPackage(packageName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    public static IEnumerable<object[]> __CreatePackageActionNoConflict()
    {
        yield return new object[] { new CreatePackageAction()
        {
            PackageActionType = PackageActionType.Email
        }};
    }

    [Theory]
    [MemberData(nameof(__CreatePackageActionNoConflict))]
    public async Task GlobalCreatePackageActionById(CreatePackageAction createPackageAction)
    {
        const string packageId = "1";

        var result = await _validMocks.CreatePackageAction(packageId, createPackageAction);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreatePackageActionNoConflict))]
    public async Task GlobalCreatePackageActionByName(CreatePackageAction createPackageAction)
    {
        const string packageName = "one";

        var result = await _validMocks.CreatePackageAction(packageName, createPackageAction);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalGetPackageActionsByPackageId(string packageActionId)
    {
        var result = await _validMocks.GetPackageActions(packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalGetPackageActionsByPackageName(string packageActionName)
    {
        var result = await _validMocks.GetPackageActions(packageActionName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "1")]
    public async Task GlobalGetPackageActionByIdId(string packageId, string packageActionId)
    {
        var result = await _validMocks.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "one")]
    public async Task GlobalGetPackageActionByNameName(string packageName, string packageActionName)
    {
        var result = await _validMocks.GetPackageAction(packageName, packageActionName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "1")]
    public async Task GlobalGetPackageActionByNameId(string packageId, string packageActionId)
    {
        var result = await _validMocks.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    [Theory]
    [InlineData("1", "one")]
    public async Task GlobalGetPackageActionByIdName(string packageId, string packageActionId)
    {
        var result = await _validMocks.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    public static IEnumerable<object[]> __ActOnPackageActionNoConflict()
    {
        yield return new object[] 
        { 
            new ActPackageAction()
            {
                Action = "search",
                Filter = "attachment"
            }, new ActPackageAction()
            {
                Action = "search",
                Email = new()
                {
                    Search = "attachment"
                }
            }, new ActPackageAction()
            {
                Action = "viewEmail",
                Filter = "attachment"
            }, new ActPackageAction()
            {
                Action = "viewemail",
                Email = new()
                {
                    Search = "attachment"
                }
            }, new ActPackageAction()
            {
                Action = "upload",
                Email = new()
                {
                    File = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Data/testing.eml"))
                }
            }, new ActPackageAction()
            {
                Action = "addAttachments",
                Email = new()
                {
                    Attachments = new []
                    {
                        new EmailAttachment()
                        {
                            AttachmentBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Data/attachment1.txt")),
                            MimeType = "text/plain",
                            Name = "attachment1.txt"
                        },
                        new EmailAttachment()
                        {
                            AttachmentBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Data/attachment2.pdf")),
                            MimeType = "application/pdf",
                            Name = "attachment2.pdf"
                        }
                    },
                    EmailId = 1
                }
            }, new ActPackageAction()
            {
                Action = "remove",
                Email = new()
                {
                    EmailId = 1
                }
            },
            new ActPackageAction()
            {
                Action = "1",
                Filter = "attachment"
            }, new ActPackageAction()
            {
                Action = "1",
                Email = new()
                {
                    Search = "attachment"
                }
            }, new ActPackageAction()
            {
                Action = "2",
                Filter = "attachment"
            }, new ActPackageAction()
            {
                Action = "2",
                Email = new()
                {
                    Search = "attachment"
                }
            }, new ActPackageAction()
            {
                Action = "3",
                Email = new()
                {
                    File = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Data/testing.eml"))
                }
            }, new ActPackageAction()
            {
                Action = "4",
                Email = new()
                {
                    Attachments = new []
                    {
                        new EmailAttachment()
                        {
                            AttachmentBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Data/attachment1.txt")),
                            MimeType = "text/plain",
                            Name = "attachment1.txt"
                        },
                        new EmailAttachment()
                        {
                            AttachmentBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Data/attachment2.pdf")),
                            MimeType = "application/pdf",
                            Name = "attachment2.pdf"
                        }
                    },
                    EmailId = 1
                }
            }, new ActPackageAction()
            {
                Action = "5",
                Email = new()
                {
                    EmailId = 1
                }
            }
        };
    }

    [Theory]
    [MemberData(nameof(__ActOnPackageActionNoConflict))]
    public async Task GlobalActOnPackageAction1(params ActPackageAction[] acts)
    {
        const string packageId = "1";
        const string packageActionId = "1";

        foreach (var act in acts)
        {
            var result = await _validMocks.ActOnPackageAction(packageId, packageActionId, act);
        
            Assert.True(result.Success, $"{act.Action} ({act.Filter}; {act.Email?.Search}):: {result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
        }
    }

    [Theory]
    [MemberData(nameof(__ActOnPackageActionNoConflict))]
    public async Task GlobalActOnPackageAction2(params ActPackageAction[] acts)
    {
        const string packageId = "one";
        const string packageActionId = "1";

        foreach (var act in acts)
        {
            var result = await _validMocks.ActOnPackageAction(packageId, packageActionId, act);
        
            Assert.True(result.Success, $"{act.Action} ({act.Filter}; {act.Email?.Search}):: {result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
        }
    }
    
    
    [Theory]
    [MemberData(nameof(__ActOnPackageActionNoConflict))]
    public async Task GlobalActOnPackageAction5(params ActPackageAction[] acts)
    {
        const string packageId = "1";
        const string packageActionId = "email";

        foreach (var act in acts)
        {
            var result = await _validMocks.ActOnPackageAction(packageId, packageActionId, act);
        
            Assert.True(result.Success, $"{act.Action} ({act.Filter}; {act.Email?.Search}):: {result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
        }
    }
    #endregion
}