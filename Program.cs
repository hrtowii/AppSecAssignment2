using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Assignment2;
using Assignment2.Models;
using Assignment2.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Resend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure MyDbContext with a SQL connection using the connection string from configuration.
// Adjust to your database provider. Here, SQL Server is used as an example.
var connectionString = builder.Configuration.GetConnectionString("MyConnection");
builder.Services.AddDbContext<MyDbContext>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Registration/Login";
        options.AccessDeniedPath = "/Home/Error";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.Configure<ResendClientOptions>( o =>
{
    o.ApiToken = "re_ap5ZjLHv_4FvEMTAiDKp8drSLvA94fiZA";
} );
builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddTransient<IResend, ResendClient>();
// dependency injection
builder.Services.AddTransient<EmailService>();
builder.Services.AddTransient<RedisService>();
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Adjust for development
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Bind Google reCAPTCHA settings
builder.Services.Configure<GoogleRecaptchaSettings>(builder.Configuration.GetSection("GoogleRecaptcha"));
var app = builder.Build();
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithRedirects("/Home/Error?statusCode={0}");
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// // Enable Swagger (this can be limited to development if required).
// app.UseSwagger();
// app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SessionValidationMiddleware>();

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
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    // Updated CSP to allow necessary resources
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com https://www.gstatic.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "frame-src 'self' https://www.google.com; " +
        "connect-src 'self' https://www.google.com; " +
        "font-src 'self' data:;"
    );
    
    await next();
});
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Home") && 
        !context.Request.Path.Equals("/Home/Login") &&
        !context.User.Identity.IsAuthenticated)
    {
        context.Response.Redirect("/Login");
        return;
    }
    await next();
});
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.Run();