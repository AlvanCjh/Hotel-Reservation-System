global using Demo.Models;
using Demo.Options;
using Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// Email
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection("SendGrid"));

// For the ApiKey from user-secrets or environment:
builder.Configuration.AddEnvironmentVariables();

// Register email service
builder.Services.AddSingleton<IEmailSender, SendGridEmailSender>();

// Authentication
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Account/MemberLogin"; // Changed from "/Account/Login"
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Authorization
builder.Services.AddAuthorization();

builder.Services.AddSession();

builder.Services.AddControllersWithViews();

builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");

builder.Services.Configure<GoogleReCaptchaSettings>(
    builder.Configuration.GetSection("GoogleReCaptcha")
);

var app = builder.Build();

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
