using AppliedSoftware.Extensions;
using AppliedSoftware.Workers.EFCore;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;

namespace AppliedSoftware.Workers;

public sealed class FirebaseUserSync(
    IServiceProvider serviceProvider,
    Settings config, 
    ILogger<FirebaseUserSync> logger)
{
    // keeping track of how many times the sync has failed.
    private int _failureCount = 0;
    
    private FirebaseAuth? _auth;
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Firebase user sync worker started");
        
        _auth = FirebaseAuth.DefaultInstance;
    }

    private async Task SyncUsers(CancellationToken ct)
    {
        logger.LogInformation("Starting user sync task");
        
        
        while (!ct.IsCancellationRequested)
        {
            logger.LogInformation("Running user sync");

            var context = serviceProvider.CreateScope()
                            .ServiceProvider
                            .GetRequiredService<ExtranetContext>();
            try
            {
                if (_auth is null)
                    throw new AuthenticationFailureException("Firebase authenticator has not been initialized");

                var userOptions = new ListUsersOptions()
                {
                    PageSize = 500
                };
                
                var usersPagedEnumerable = _auth.ListUsersAsync(userOptions).AsRawResponses();

                
                await foreach (var usersPage in usersPagedEnumerable)
                {
                    // logger.LogInformation($"Saving {users.Count} users in current page");
                    //
                    // foreach (var user in users)
                    // {   // TODO: Implement sync; also need to handle the case where the user already exists and has changes upstream as well as local.
                    //     context.Users.Add(user);
                    //     await context.SaveChangesAsync(ct);
                    // }
                }

            }
            catch (AuthenticationFailureException ex)
            {
                logger.LogError(ex, "Firebase authenticator has not been initialized");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error syncing users");
                _failureCount++;
            }

            await Task.Delay(CalculateNextRun(), ct);
        }
        
    }

    private TimeSpan CalculateNextRun()
    {
        var timeOfDay = DateTime.Now.TimeOfDay;
        var nextTimeRun = timeOfDay.Add(TimeSpan.FromMinutes(config.FirebaseSettings.UserPollIntervalInMinutes));
        return nextTimeRun - timeOfDay;
    }
}