
using AppliedSoftware;
using AppliedSoftware.Extensions;
using AppliedSoftware.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var firebaseSettings = new FirebaseSettings();
builder.Configuration.GetSection("Jwt").Bind(firebaseSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = "https://securetoken.google.com/pageturner-bookstore";
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "https://securetoken.google.com/pageturner-bookstore",
        ValidAudience = "pageturner-bookstore"
    };
});

builder.Services.AddSingleton<IAuthentication, Authentication>();

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

app.Run();
