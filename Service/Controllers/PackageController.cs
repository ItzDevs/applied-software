using AppliedSoftware.Models.Request;
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
    [HttpPost]
    public async Task<IActionResult> CreatePackage(
        CreatePackage createPackage)
        => (await repository.CreatePackage(createPackage)).ToResponse();
    
    /// <summary>
    /// Gets all packages that a user has authorised access to.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetPackages()
        => (await repository.GetPackages()).ToResponse();
    
    [HttpGet("{packageIdentifier}")]
    public async Task<IActionResult> GetPackage(string packageIdentifier) 
        => (await repository.GetPackage(packageIdentifier)).ToResponse();

    [HttpGet("{packageIdentifier}/actions")]
    public async Task<IActionResult> GetPackageActions(string packageIdentifier) =>
        (await repository.GetPackageActions(packageIdentifier)).ToResponse();
    
    [HttpPost("{packageIdentifier}/actions")]
    public async Task<IActionResult> CreatePackageAction(string packageIdentifier, CreatePackageAction createPackageAction)
        => (await repository.CreatePackageAction(packageIdentifier, createPackageAction)).ToResponse();
    
    [HttpGet("{packageIdentifier}/actions/{packageActionIdentifier}")]
    public async Task<IActionResult> GetPackageAction(string packageIdentifier, string packageActionIdentifier)
        => (await repository.GetPackageAction(packageIdentifier, packageActionIdentifier)).ToResponse();
    
    [HttpPost("{packageIdentifier}/actions/{packageActionIdentifier}/act")]
    public async Task<IActionResult> Act(
        string packageIdentifier, 
        string packageActionIdentifier, 
        ActPackageAction packageAction) 
        => (await repository.ActOnPackageAction(packageIdentifier, packageActionIdentifier, packageAction)).ToResponse();
}