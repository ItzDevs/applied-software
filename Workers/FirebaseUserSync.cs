using AppliedSoftware.Extensions;
using AppliedSoftware.Workers.EFCore;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace AppliedSoftware.Workers;

public sealed class FirebaseUserSync(
    IServiceProvider serviceProvider,
    Settings config, 
    ILogger<FirebaseUserSync> logger) : IFirebaseUserSync
{
    // keeping track of how many times the sync has failed.
    private int _failureCount = 0;

    private CancellationToken _cancellationToken;
    
    private FirebaseAuth? _auth;
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Firebase user sync worker started");

        _cancellationToken = ct;
        
        _auth = FirebaseAuth.DefaultInstance;

        await Task.Factory.StartNew(SyncUsers, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private async Task SyncUsers()
    {
        logger.LogInformation("Starting user sync task");
        
        
        while (!_cancellationToken.IsCancellationRequested)
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

                // This is a tracked list - any changes can be applied to these user objects then saved to the database.
                var previouslyMergedUsers 
                    = await context.Users
                        .Where(x => !x.Deleted)
                        .ToListAsync(_cancellationToken);
                
                await foreach (var usersPage in usersPagedEnumerable)
                {
                    var currentUsers = usersPage.Users.ToList();
                    
                    // Tracking changes to the existing users.
                    var mergedUsersInPage = previouslyMergedUsers
                        .Where(u => currentUsers.Any(cu => cu.Uid == u.Uid)).ToList();
                    
                    foreach (var mergedUser in mergedUsersInPage)
                    {
                        var currentUserFirebase = currentUsers.FirstOrDefault(y => y.Uid == mergedUser.Uid);

                        #region Display Name
                        // Checking the local firebase display name to the remote "upstream" display name, 
                        // using the email as a fallback if there is no display name (the same behaviour as saving a user with no set display name).
                        var trackedUpstreamChangeDisplayName =
                            !mergedUser.FirebaseDisplayName?.Equals(currentUserFirebase?.DisplayName ??
                                                                    currentUserFirebase?.Email);
                        // Now we need to check if the local display name has been set independently of the firebase display name
                        // which needs to be done prior to any potential modifications to the names to ensure this is correct.
                        var locallyModifiedDisplayName = !mergedUser.DisplayName.Equals(mergedUser.FirebaseDisplayName);
                        // Now for the actual modifications to users with a changed display name.
                        if (trackedUpstreamChangeDisplayName == true)
                        {   // Updating the firebase display name property.
                            logger.LogInformation($"Upstream display name changed for user {mergedUser.Uid}");
                            mergedUser.FirebaseDisplayName = currentUserFirebase?.DisplayName ?? 
                                                             mergedUser.FirebaseDisplayName; // If display name above is null, use their old username.
                            mergedUser.UpdatedAtUtc = DateTime.UtcNow;
                        
                            // The user has a locally modified display name, any updates to the DisplayName property should not be applied.
                            if (locallyModifiedDisplayName)
                            {
                                logger.LogInformation("Local display name was overriden, skipping updating property");
                            }
                            else
                            {
                                logger.LogInformation($"Upstream display name changed for user {mergedUser.Uid}; syncing");
                                try
                                {
                                    mergedUser.DisplayName = currentUserFirebase?.DisplayName ?? 
                                                             throw new Exception("No display name to sync");
                                    mergedUser.UpdatedAtUtc = DateTime.UtcNow;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, ex.Message);
                                }
                            }
                        }
                        #endregion
                        
                        #region Disabled status
                        var trackedUpstreamChangeDisabledStatus = mergedUser.FirebaseDisabled != currentUserFirebase?.Disabled;
                        var locallyModifiedDisabledStatus = mergedUser.Disabled != mergedUser.FirebaseDisabled;

                        if (trackedUpstreamChangeDisabledStatus)
                        {
                            if (mergedUser.FirebaseDisabled != currentUserFirebase?.Disabled)
                            {
                                logger.LogInformation($"Upstream disabled status has changed for user {mergedUser.Uid}");
                                mergedUser.FirebaseDisabled = currentUserFirebase?.Disabled ?? 
                                                              true; // If the disabled status is null, assume the user is disabled.
                                mergedUser.UpdatedAtUtc = DateTime.UtcNow;
                            }

                            // The user has a locally modified disabled status, any updates to the status should not be applied.
                            if (locallyModifiedDisabledStatus)
                            {
                                logger.LogInformation("Disabled status was overridden, skipping updating property");
                            }
                            else
                            {
                                logger.LogInformation("Syncing the upstream disabled status to the local status");
                                try
                                {
                                    mergedUser.Disabled = currentUserFirebase?.Disabled ?? 
                                                          mergedUser.Disabled; // If the disabled status above is null, fallback to their old status.
                                    mergedUser.UpdatedAtUtc = DateTime.UtcNow;
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, ex.Message);
                                }
                            }
                        }
                        #endregion
                    }
                    
                    // Adding new users
                    var newUsersInPage =
                        currentUsers.Where(cu => !previouslyMergedUsers.Select(x => x.Uid).Contains(cu.Uid)).ToList();
                    await context.Users.AddRangeAsync(newUsersInPage.ToUserDtos(), _cancellationToken);
                    
                    // Soft-deleting deleted users.
                    var deletedUsersInPage = 
                        previouslyMergedUsers.Where(u => !currentUsers.Select(x => x.Uid).Contains(u.Uid)).ToList();

                    foreach (var deletedUser in deletedUsersInPage)
                    {
                        logger.LogInformation($"Soft-deleting user {deletedUser.Uid}");
                        deletedUser.Deleted = true;
                        deletedUser.UpdatedAtUtc = DateTime.UtcNow;
                    }
                    await context.SaveChangesAsync(_cancellationToken);
                }
            }
            catch (AuthenticationFailureException ex)
            {
                logger.LogError(ex, "Firebase authenticator has not been initialised");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error syncing users");
                _failureCount++;
            }
            await Task.Delay(CalculateNextRun(), _cancellationToken);
        }
    }

    private TimeSpan CalculateNextRun()
    {
        var timeOfDay = DateTime.Now.TimeOfDay;
        var nextTimeRun = timeOfDay.Add(TimeSpan.FromMinutes(config.FirebaseSettings.UserPollIntervalInMinutes));
        return nextTimeRun - timeOfDay;
    }
}