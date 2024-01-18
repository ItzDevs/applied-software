using AppliedSoftware.Models;
using AppliedSoftware.Models.Response;
using MediatR;

namespace AppliedSoftware.Workers.Handlers;

public class CdnDownloadHandler(
    IRepository repository,
    ILogger<CdnDownloadHandler> logger) : IRequestHandler<GetFileByIdQuery, StatusContainer<(Stream, string, string)>>
{
    // This is a bit anti-pattern as it essentially just adds a layer of indirection, however the logic would be the same anyway.
    public async Task<StatusContainer<(Stream, string, string)>> Handle(GetFileByIdQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation(nameof(CdnDownloadHandler));
        return await repository.DownloadAttachment(request.Id);
    }
}