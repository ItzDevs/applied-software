using System.Net;
using System.Runtime.Serialization;
using AppliedSoftware.Models.Enums;
using AppliedSoftware.Models.Request;
using AppliedSoftware.Models.Response;
using AppliedSoftwareTests.Mock;
using Xunit.Sdk;

namespace AppliedSoftwareTests;

public class InvalidTests
{
    // A valid team, which also conflict for the UpdateTeam 
    public static IEnumerable<object[]> __CreateTeamConflict()
    {
        yield return new object[] { new CreateTeam
        {
            Name = "example",
            Description = "example",
            DefaultAllowedPermissions = PackageUserPermission.DefaultRead
        }};
    }
    
    [Theory]
    [MemberData(nameof(__CreateTeamConflict))]
    public async Task UpdateTeamConflict(CreateTeam updateTeam)
    {
        var mock = new MockRepository(GlobalPermission.Administrator, PackageUserPermission.None);
        const string teamName = "one";
        var result = await mock.UpdateTeam(teamName, updateTeam);
        
        Assert.True(result.StatusCode == HttpStatusCode.BadRequest, $"{result.StatusCode} - Error: {result.ResponseData.Error?.Code} (no messages are returned).");
        Assert.True(((CodeMessageResponse) result.ResponseData!.Error!).Code == (int)eErrorCode.Conflict, $"{result.StatusCode} - Error: {result.ResponseData?.Error?.Code} (no)");
    }
}