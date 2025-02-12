using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Assignment2;
using Assignment2.Services;
using Resend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure MyDbContext with a SQL connection using the connection string from configuration.
// Adjust to your database provider. Here, SQL Server is used as an example.
var connectionString = builder.Configuration.GetConnectionString("MyConnection");
builder.Services.AddDbContext<MyDbContext>();

// Configure JWT Authentication.
var secret = builder.Configuration.GetValue<string>("Authentication:Secret");
if (string.IsNullOrEmpty(secret))
{
    throw new Exception("Secret is required for JWT authentication.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,           // Change to true if you need to validate the issuer.
            ValidateAudience = false,         // Change to true if you need to validate the audience.
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

// Configure Swagger/OpenAPI with JWT support.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid JWT token with Bearer scheme",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer",
        // Reference = new OpenApiReference
        // {
        //     Type = ReferenceType.SecurityScheme,
        //     Id = "Bearer"
        // }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    // options.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     { securityScheme, new List<string>() }
    // });
});
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    // o.ApiToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN")!;
    o.ApiToken = "re_jgTWYyQW_7G1sHAzLCGVgfwDHQRTz5AfC";
});
builder.Services.AddTransient<IResend, ResendClient>();
// dependency injection
builder.Services.AddTransient<EmailService>();
builder.Services.AddTransient<RedisService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Enable Swagger (this can be limited to development if required).
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add Authentication and Authorization middleware.
app.UseAuthentication();
app.UseAuthorization();

// Map custom routes for registration and login.
app.MapControllerRoute(
    name: "register",
    pattern: "register",
    defaults: new { controller = "Registration", action = "Register" }
);
app.MapControllerRoute(
    name: "login",
    pattern: "login",
    defaults: new { controller = "Registration", action = "Login" }
);

// Default routing.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();