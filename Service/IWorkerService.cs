namespace AppliedSoftware;

public interface IWorkerService
{
    Task StartAsync(CancellationToken ct = default);
}