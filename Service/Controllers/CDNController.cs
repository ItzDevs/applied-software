using AppliedSoftware.Models;
using AppliedSoftware.Workers;
using AppliedSoftware.Workers.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Controllers;

[Route("api/v1/cdn")]
public class CdnController(
    IMediator mediator,
    IRepository repository) : ControllerBase
{
    // In an ideal world, this would be separated, rather than using the service to return the bytes, 
    // a pre-signed URL would be returned from elsewhere in the service (e.g., /api/v1/package/act) when returning emails
    // that points to an AWS S3/CloudFlare R2 bucket; this would also be less strenuous on the service as it then wouldn't
    // need to handle as much traffic.
    [HttpGet("download/attachment/{id:long}")]
    public async Task<IActionResult> DownloadAttachment(long id)
    {
        var file = await mediator.Send(new GetFileByIdQuery { Id = id });
        
        if (!file.Success) 
            return BadRequest(file.ResponseData.Error);
        file.ResponseData.Body.Item1.Position = 0;
        return File(file.ResponseData.Body.Item1, 
            file.ResponseData.Body.Item2, 
            file.ResponseData.Body.Item3);
    }
}