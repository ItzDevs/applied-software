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
    
    public static IEnumerable<object[]> __CreatePackageNoConflict()
    {
        yield return new object[] { new CreatePackage()
        {
            Name = "Test User Group",
            Description = "This is a test user group"
        }};
    }
    
    public static IEnumerable<object[]> __CreatePackageActionNoConflict()
    {
        yield return new object[] { new CreatePackageAction()
        {
            PackageActionType = PackageActionType.Email
        }};
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

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalGetTeamId(string teamId)
    {
        var result = await _validMocks.GetTeam(teamId);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalGetTeamName(string teamName)
    {
        var result = await _validMocks.GetTeam(teamName);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
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

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalDeleteTeamByName(string teamName)
    {
        var result = await _validMocks.DeleteTeam(teamName);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "123456,123")]
    public async Task GlobalAddUsersToTeamById(string teamId, string userIds)
    {
        var result = await _validMocks.AddUsersToTeam(teamId, userIds);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "two,one")]
    public async Task GlobalAddUsersToTeamByName(string teamId, string userIds)
    {
        var result = await _validMocks.AddUsersToTeam(teamId, userIds);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "123456,one")]
    public async Task GlobalRemoveUsersFromTeamById(string teamId, string userIds)
    {
        var result = await _validMocks.RemoveUsersFromTeam(teamId, userIds);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "123456,one")]
    public async Task GlobalRemoveUsersFromTeamByName(string teamId, string userIds)
    {
        var result = await _validMocks.RemoveUsersFromTeam(teamId, userIds);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateUserGroupNoConflict))]
    public async Task GlobalCreateUserGroup(CreateUserGroup createUserGroup)
    {
        var result = await _validMocks.CreateUserGroup(createUserGroup);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Fact]
    public async Task GlobalGetUserGroups()
    {
        var result = await _validMocks.GetUserGroups();

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalGetUserGroupById(string userGroupId)
    {
        var result = await _validMocks.GetUserGroup(userGroupId);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalGetUserGroupName(string userGroupName)
    {
        var result = await _validMocks.GetUserGroup(userGroupName);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateUserGroupNoConflict))]
    public async Task GlobalUpdateUserGroupById(CreateUserGroup updateUserGroup)
    {
        const string userGroupId = "1";
        var result = await _validMocks.UpdateUserGroup(userGroupId, updateUserGroup);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
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

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalDeleteUserGroupByName(string userGroupName)
    {
        var result = await _validMocks.DeleteUserGroup(userGroupName);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "two,one")]
    public async Task GlobalAddUsersToUserGroupById(string userGroupId, string userIds)
    {
        var result = await _validMocks.AddUsersToUserGroup(userGroupId, userIds);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "two,one")]
    public async Task GlobalAddUsersToUserGroupByName(string userGroupId, string userIds)
    {
        var result = await _validMocks.AddUsersToUserGroup(userGroupId, userIds);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "two,one")]
    public async Task GlobalRemoveUsersFromUserGroupById(string userGroupId, string userIds)
    {
        var result = await _validMocks.RemoveUsersFromUserGroup(userGroupId, userIds);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "two,one")]
    public async Task GlobalRemoveUsersFromUserGroupByName(string userGroupId, string userIds)
    {
        var result = await _validMocks.RemoveUsersFromUserGroup(userGroupId, userIds);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
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

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task GlobalGetPackageById(string packageId)
    {
        var result = await _validMocks.GetPackage(packageId);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task GlobalGetPackageByName(string packageName)
    {
        var result = await _validMocks.GetPackage(packageName);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreatePackageActionNoConflict))]
    public async Task GlobalCreatePackageActionById(CreatePackageAction createPackageAction)
    {
        const string packageId = "1";

        var result = await _validMocks.CreatePackageAction(packageId, createPackageAction);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
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

    [Theory]
    [MemberData(nameof(__ActOnPackageActionNoConflict))]
    public async Task GlobalActOnPackageAction1(params ActPackageAction[] acts)
    {
        const string packageId = "1";
        const string packageActionId = "1";

        foreach (var act in acts)
        {
            var result = await _validMocks.ActOnPackageAction(packageId, packageActionId, act);

            Assert.True(result.Success,
                $"{act.Action} ({act.Filter}; {act.Email?.Search}):: {result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
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
    
    #region Using required permissions.
    [Theory]
    [MemberData(nameof(__CreateTeamNoConflict))]
    public async Task RequiredCreateTeam(CreateTeam createTeam)
    {
        var mock = new MockRepository(GlobalPermission.CreateTeam, PackageActionPermission.None);
        var result = await mock.CreateTeam(createTeam);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Fact]
    public async Task RequiredGetTeamsGlobalPermission()
    {
        var mock = new MockRepository(GlobalPermission.ReadTeam, PackageActionPermission.None);
        var result = await mock.GetTeams();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Fact]
    public async Task RequiredGetTeamsUserInTeam()
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetTeams();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task RequiredGetTeamIdGlobalPermission(string teamId)
    {
        var mock = new MockRepository(GlobalPermission.ReadTeam, PackageActionPermission.None);
        var result = await mock.GetTeam(teamId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("1")]
    public async Task RequiredGetTeamIdGlobalPermissionInTeam(string teamId)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetTeam(teamId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task RequiredGetTeamNameGlobalPermission(string teamName)
    {
        var mock = new MockRepository(GlobalPermission.ReadTeam, PackageActionPermission.None);
        var result = await mock.GetTeam(teamName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task RequiredGetTeamNameGlobalPermissionInTeam(string teamName)
    {
        var mock = new MockRepository(GlobalPermission.ReadTeam, PackageActionPermission.None);
        var result = await mock.GetTeam(teamName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateTeamNoConflict))]
    public async Task RequiredUpdateTeamById(CreateTeam updateTeam)
    {
        var mock = new MockRepository(GlobalPermission.ModifyTeam, PackageActionPermission.None);
        const string teamId = "1";
        var result = await mock.UpdateTeam(teamId, updateTeam);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateTeamNoConflict))]
    public async Task RequiredUpdateTeamByName(CreateTeam updateTeam)
    {
        var mock = new MockRepository(GlobalPermission.ModifyTeam, PackageActionPermission.None);
        const string teamName = "one";
        var result = await mock.UpdateTeam(teamName, updateTeam);
        
        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task RequiredDeleteTeamById(string teamId)
    {
        var mock = new MockRepository(GlobalPermission.DeleteTeam, PackageActionPermission.None);
        var result = await mock.DeleteTeam(teamId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one")]
    public async Task RequiredDeleteTeamByName(string teamName)
    {
        var mock = new MockRepository(GlobalPermission.DeleteTeam, PackageActionPermission.None);
        var result = await mock.DeleteTeam(teamName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "123456,123")]
    public async Task RequiredAddUsersToTeamById(string teamId, string userIds)
    {
        var mock = new MockRepository(GlobalPermission.AddUserToTeam, PackageActionPermission.None);
        var result = await mock.AddUsersToTeam(teamId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one", "two,one")]
    public async Task RequiredAddUsersToTeamByName(string teamId, string userIds)
    {
        var mock = new MockRepository(GlobalPermission.AddUserToTeam, PackageActionPermission.None);
        var result = await mock.AddUsersToTeam(teamId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "123456,one")]
    public async Task RequiredRemoveUsersFromTeamById(string teamId, string userIds)
    {
        var mock = new MockRepository(GlobalPermission.RemoveUserFromTeam, PackageActionPermission.None);
        var result = await mock.RemoveUsersFromTeam(teamId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "123456,one")]
    public async Task RequiredRemoveUsersFromTeamByName(string teamId, string userIds)
    {
        var mock = new MockRepository(GlobalPermission.RemoveUserFromTeam, PackageActionPermission.None);
        var result = await mock.RemoveUsersFromTeam(teamId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateUserGroupNoConflict))]
    public async Task RequiredCreateUserGroup(CreateUserGroup createUserGroup)
    {
        var mock = new MockRepository(GlobalPermission.CreateUserGroup, PackageActionPermission.None);
        var result = await mock.CreateUserGroup(createUserGroup);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Fact]
    public async Task RequiredGetUserGroupsGlobalPermission()
    {
        var mock = new MockRepository(GlobalPermission.ReadUserGroup, PackageActionPermission.None);
        var result = await mock.GetUserGroups();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Fact]
    public async Task RequiredGetUserGroupsUser()
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetUserGroups();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task RequiredGetUserGroupByIdGlobalPermission(string userGroupId)
    {
        var mock = new MockRepository(GlobalPermission.ReadUserGroup, PackageActionPermission.None);
        var result = await mock.GetUserGroup(userGroupId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("1")]
    public async Task RequiredGetUserGroupByIdUser(string userGroupId)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetUserGroup(userGroupId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task RequiredGetUserGroupName(string userGroupName)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetUserGroup(userGroupName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateUserGroupNoConflict))]
    public async Task RequiredUpdateUserGroupById(CreateUserGroup updateUserGroup)
    {
        var mock = new MockRepository(GlobalPermission.ModifyUserGroup, PackageActionPermission.None);
        const string userGroupId = "1";
        var result = await mock.UpdateUserGroup(userGroupId, updateUserGroup);

        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreateUserGroupNoConflict))]
    public async Task RequiredUpdateUserGroupByName(CreateUserGroup updateUserGroup)
    {
        var mock = new MockRepository(GlobalPermission.ModifyUserGroup, PackageActionPermission.None);
        const string userGroupName = "one";
        var result = await mock.UpdateUserGroup(userGroupName, updateUserGroup);
        
        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task RequiredDeleteUserGroupById(string userGroupId)
    {
        var mock = new MockRepository(GlobalPermission.DeleteUserGroup, PackageActionPermission.None);
        var result = await mock.DeleteUserGroup(userGroupId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task RequiredDeleteUserGroupByName(string userGroupName)
    {
        var mock = new MockRepository(GlobalPermission.DeleteUserGroup, PackageActionPermission.None);
        var result = await mock.DeleteUserGroup(userGroupName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "two,one")]
    public async Task RequiredAddUsersToUserGroupById(string userGroupId, string userIds)
    {
        var mock = new MockRepository(GlobalPermission.AddUserToGroup, PackageActionPermission.None);
        var result = await mock.AddUsersToUserGroup(userGroupId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "two,one")]
    public async Task RequiredAddUsersToUserGroupByName(string userGroupId, string userIds)
    {
        var mock = new MockRepository(GlobalPermission.AddUserToGroup, PackageActionPermission.None);
        var result = await mock.AddUsersToUserGroup(userGroupId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "two,one")]
    public async Task RequiredRemoveUsersFromUserGroupById(string userGroupId, string userIds)
    {
        var mock = new MockRepository(GlobalPermission.RemoveUserFromGroup, PackageActionPermission.None);
        var result = await mock.RemoveUsersFromUserGroup(userGroupId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "two,one")]
    public async Task RequiredRemoveUsersFromUserGroupByName(string userGroupId, string userIds)
    {
        var mock = new MockRepository(GlobalPermission.RemoveUserFromGroup, PackageActionPermission.None);
        var result = await mock.RemoveUsersFromUserGroup(userGroupId, userIds);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreatePackageNoConflict))]
    public async Task RequiredCreatePackage(CreatePackage createPackage)
    {
        var mock = new MockRepository(GlobalPermission.CreatePackage, PackageActionPermission.None);
        var result = await mock.CreatePackage(createPackage);

        Assert.True(result.Success,
            $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Fact]
    public async Task RequiredGetPackagesGlobalPermission()
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackages();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Fact]
    public async Task RequiredGetPackagesUser()
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackages();
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task RequiredGetPackageByIdGlobalPermission(string packageId)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackage(packageId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("1")]
    public async Task RequiredGetPackageByIdUser(string packageId)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackage(packageId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task RequiredGetPackageByNameGlobalPermission(string packageName)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackage(packageName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one")]
    public async Task RequiredGetPackageByNameUser(string packageName)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackage(packageName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreatePackageActionNoConflict))]
    public async Task RequiredCreatePackageActionById(CreatePackageAction createPackageAction)
    {
        var mock = new MockRepository(GlobalPermission.ModifyPackage, PackageActionPermission.None);
        const string packageId = "1";

        var result = await mock.CreatePackageAction(packageId, createPackageAction);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__CreatePackageActionNoConflict))]
    public async Task RequiredCreatePackageActionByName(CreatePackageAction createPackageAction)
    {
        var mock = new MockRepository(GlobalPermission.ModifyPackage, PackageActionPermission.None);
        const string packageName = "one";

        var result = await mock.CreatePackageAction(packageName, createPackageAction);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1")]
    public async Task RequiredGetPackageActionsByPackageIdGlobalPermission(string packageActionId)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackageActions(packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    [Theory]
    [InlineData("1")]
    public async Task RequiredGetPackageActionsByPackageIdUser(string packageActionId)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackageActions(packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one")]
    public async Task RequiredGetPackageActionsByPackageNameGlobalPermission(string packageActionName)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackageActions(packageActionName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one")]
    public async Task RequiredGetPackageActionsByPackageNameUser(string packageActionName)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackageActions(packageActionName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("1", "1")]
    public async Task RequiredGetPackageActionByIdIdGlobalPermission(string packageId, string packageActionId)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("1", "1")]
    public async Task RequiredGetPackageActionByIdIdUser(string packageId, string packageActionId)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one", "one")]
    public async Task RequiredGetPackageActionByNameNameGlobalPermission(string packageName, string packageActionName)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackageAction(packageName, packageActionName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one", "one")]
    public async Task RequiredGetPackageActionByNameNameUser(string packageName, string packageActionName)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackageAction(packageName, packageActionName);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [InlineData("one", "1")]
    public async Task RequiredGetPackageActionByNameIdGlobalPermission(string packageId, string packageActionId)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("one", "1")]
    public async Task RequiredGetPackageActionByNameIdUser(string packageId, string packageActionId)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    
    [Theory]
    [InlineData("1", "one")]
    public async Task RequiredGetPackageActionByIdNameGlobalPermission(string packageId, string packageActionId)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, PackageActionPermission.None);
        var result = await mock.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }
    
    [Theory]
    [InlineData("1", "one")]
    public async Task RequiredGetPackageActionByIdNameUser(string packageId, string packageActionId)
    {
        var mock = new MockRepository(GlobalPermission.None, PackageActionPermission.None, true);
        var result = await mock.GetPackageAction(packageId, packageActionId);
        
        Assert.True(result.Success, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
    }

    [Theory]
    [MemberData(nameof(__ActOnPackageActionNoConflict))]
    public async Task RequiredActOnPackageAction1(params ActPackageAction[] acts)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, 
            PackageActionPermission.DefaultRead | 
            PackageActionPermission.AddSelf | 
            PackageActionPermission.UpdateAlt | PackageActionPermission.UpdateSelf |
            PackageActionPermission.DeleteAlt | PackageActionPermission.DeleteSelf, true);
        const string packageId = "1";
        const string packageActionId = "1";

        foreach (var act in acts)
        {
            var result = await mock.ActOnPackageAction(packageId, packageActionId, act);
        
            Assert.True(result.Success, $"{act.Action} ({act.Filter}; {act.Email?.Search}):: {result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
        }
    }

    [Theory]
    [MemberData(nameof(__ActOnPackageActionNoConflict))]
    public async Task RequiredActOnPackageAction2(params ActPackageAction[] acts)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, 
            PackageActionPermission.DefaultRead | 
            PackageActionPermission.AddSelf | 
            PackageActionPermission.UpdateAlt | PackageActionPermission.UpdateSelf |
            PackageActionPermission.DeleteAlt | PackageActionPermission.DeleteSelf, true);
        const string packageId = "one";
        const string packageActionId = "1";

        foreach (var act in acts)
        {
            var result = await mock.ActOnPackageAction(packageId, packageActionId, act);
        
            Assert.True(result.Success, $"{act.Action} ({act.Filter}; {act.Email?.Search}):: {result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
        }
    }
    
    
    [Theory]
    [MemberData(nameof(__ActOnPackageActionNoConflict))]
    public async Task RequiredActOnPackageAction5(params ActPackageAction[] acts)
    {
        var mock = new MockRepository(GlobalPermission.ReadPackage, 
            PackageActionPermission.DefaultRead | 
            PackageActionPermission.AddSelf | 
            PackageActionPermission.UpdateAlt | PackageActionPermission.UpdateSelf |
            PackageActionPermission.DeleteAlt | PackageActionPermission.DeleteSelf, true);
        const string packageId = "1";
        const string packageActionId = "email";

        foreach (var act in acts)
        {
            var result = await mock.ActOnPackageAction(packageId, packageActionId, act);
        
            Assert.True(result.Success, $"{act.Action} ({act.Filter}; {act.Email?.Search}):: {result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no messages are returned).");
        }
    }
    #endregion
}