
using AppliedSoftware;
using AppliedSoftware.Extensions;
using AppliedSoftware.Workers;
using AppliedSoftware.Workers.EFCore;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var serviceSettings = new Settings();
builder.Configuration.GetSection("Settings").Bind(serviceSettings);
var jwtSettings = new FirebaseJwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwtSettings);


var connStringBuilder = new ConnectionStringBuilder();
if (connStringBuilder.IsValid(out var connString))
    serviceSettings.ConnectionString = connString;

builder.Services.AddSingleton(serviceSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = jwtSettings.Authority;
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience
    };
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IAuthentication, Authentication>();
builder.Services.AddSingleton<IFirebaseUserSync, FirebaseUserSync>();
builder.Services.AddScoped<IRepository, Repository>();

builder.Services.AddNpgsql<ExtranetContext>(serviceSettings.ConnectionString, 
    opt =>
    {
        opt.MigrationsAssembly(typeof(ExtranetContext).Assembly.FullName);
    });

builder.Services.ConfigureApiVersioning();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme()
    {
        Description = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "bearer",
        Type = SecuritySchemeType.ApiKey
    });
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapControllers();

app.EnsureMigrated();


// Config loaded from GOOGLE_APPLICATION_CREDENTIALS environment variable.
FirebaseApp.Create();

var firebaseSyncService = app.Services.GetRequiredService<IFirebaseUserSync>();
await firebaseSyncService.StartAsync();


app.Run();
