using AppliedSoftware.Workers.EFCore;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AppliedSoftware.Extensions;

public static class HostBuilderExtensions
{
    public static IServiceCollection ConfigureApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(versioning =>
        {
            versioning.DefaultApiVersion = new ApiVersion(1, 0);
            versioning.AssumeDefaultVersionWhenUnspecified = true;
            versioning.ReportApiVersions = true;
        });

        return services;
    }
    /// <summary>
    /// Ensures that the running database schema is the latest.
    /// </summary>
    /// <param name="host"></param>
    public static void EnsureMigrated(this WebApplication host)
    {
        using var scope = host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        using var ctx = scope.ServiceProvider.GetRequiredService<ExtranetContext>();
        
        ctx.Database.Migrate();
    }
}