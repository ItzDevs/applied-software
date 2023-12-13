using AppliedSoftware.Models.Request.Teams;
using AppliedSoftware.Workers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Controllers;

[Asp.Versioning.ApiVersion(1)]
[ApiController]
[Route("api/v{version:apiVersion}/packages")]
[Authorize]
public class PackageController(
    IRepository repository) : ControllerBase
{
    /// <summary>
    /// Gets all packages that a user has authorised access to.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetPackages()
        => (await repository.GetPackages()).ToResponse();
    
    [HttpPost]
    public async Task<IActionResult> CreatePackage(
        CreatePackage createPackage)
        => (await repository.CreatePackage(createPackage)).ToResponse();
}