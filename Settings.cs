namespace AppliedSoftware;

public class Settings
{
    public string ConnectionString { get; set; } = string.Empty;
    public FirebaseSettings FirebaseSettings { get; set; } = new();
}

public class FirebaseSettings
{
    public double UserPollIntervalInMinutes { get; set; } = 1;
}

/// <summary>
/// A class which defines, and combines the settings to build a connection string from environment variables or being defined in Config.
/// </summary>
internal class ConnectionStringBuilder
{

    /// <summary>
    /// The postgres host (e.g., localhost).
    /// </summary>
    public string? Host { get; set; } = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "pg";

    /// <summary>
    /// The database name (e.g., vanguard-arms).
    /// </summary>
    /// <value>
    /// The database.
    /// </value>
    public string? Database { get; set; } = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");

    /// <summary>
    /// The Port of the database.
    /// </summary>
    // NOTE: Hardcoded as this is internal Docker DNS.
    public string? Port { get; set; } = "5432";

    /// <summary>
    /// The postgres username.
    /// </summary>
    public string? Username { get; set; } = Environment.GetEnvironmentVariable("POSTGRES_USER");

    /// <summary>
    /// The postgres password.
    /// </summary>
    public string? Password { get; set; } = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

    /// <summary>
    /// Whether or not to include detailed error messages.
    /// </summary>
    public string? IncludeErrorDetail { get; set; } = Environment.GetEnvironmentVariable("POSTGRES_INCLUDE_ERROR_DETAILS") ?? "False";

    /// <summary>
    /// Returns whether or not the provided details are valid.
    /// </summary>
    public bool IsValid(out string connectionString)
    {
        bool valid = this is
        {
            Host: not null,
            Database: not null,
            Username: not null,
            Password: not null,
            Port: not null,
            IncludeErrorDetail: not null
        };
        connectionString = valid ?
                            $"Server = {Host}; Database = {Database}; User Id = {Username}; Password = {Password}; Port = {Port}; Include Error Detail = {IncludeErrorDetail};" :
                            string.Empty;
        return valid;
    }
}