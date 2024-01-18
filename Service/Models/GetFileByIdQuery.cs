using AppliedSoftware.Models.Response;
using MediatR;

namespace AppliedSoftware.Workers.Handlers;

public class GetFileByIdQuery : IRequest<StatusContainer<(Stream, string, string)>>
{
    public long Id { get; set; }
}