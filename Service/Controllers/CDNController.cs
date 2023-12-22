using AppliedSoftware.Workers;
using Microsoft.AspNetCore.Mvc;

namespace AppliedSoftware.Controllers;

[Route("api/v1/cdn")]
public class CdnController(
    IRepository repository) : ControllerBase
{
    [HttpGet("download/attachment/{id:long}")]
    public async Task<IActionResult> DownloadAttachment(long id)
    {
        var file = await repository.DownloadAttachment(id);
        if (!file.Success) 
            return BadRequest(file.ResponseData.Error);
        file.ResponseData.Body.Item1.Position = 0;
        return File(file.ResponseData.Body.Item1, 
            file.ResponseData.Body.Item2, 
            file.ResponseData.Body.Item3);
    }
}