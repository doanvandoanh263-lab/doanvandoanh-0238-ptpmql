using MailKit.Net.Smtp;
using MailKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using TestMVC2026.Data;
using TestMVC2026.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment();
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 10;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<DynamicPermissionFilter>();
builder.Services.AddControllersWithViews(options =>
    options.Filters.AddService<DynamicPermissionFilter>());
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Student}/{action=Index}/{id?}");
app.MapRazorPages();

await SeedData.InitializeAsync(app.Services, app.Configuration, app.Environment);
app.Run();

public sealed class SmtpEmailSender(IConfiguration configuration, IWebHostEnvironment environment) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var host = configuration["Email:Smtp:Host"];
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        var fromAddress = configuration["Email:FromAddress"];
        var fromName = configuration["Email:FromName"] ?? "TestMVC2026";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            if (environment.IsDevelopment())
            {
                return;
            }

            throw new InvalidOperationException("SMTP host and sender address must be configured before sending email.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

        using var client = new SmtpClient();
        var port = configuration.GetValue("Email:Smtp:Port", 587);
        var useSsl = configuration.GetValue("Email:Smtp:UseSsl", false);
        await client.ConnectAsync(host, port, useSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTls);
        if (!string.IsNullOrWhiteSpace(username))
        {
            await client.AuthenticateAsync(username, password ?? string.Empty);
        }
        await client.SendAsync(message, CancellationToken.None, null);
        await client.DisconnectAsync(true);
    }
}

public sealed class DynamicPermissionFilter(ApplicationDbContext dbContext) : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> PublicActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Home.Index",
        "Home.Privacy",
        "Home.Error"
    };

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor action)
        {
            return;
        }

        var controller = action.ControllerName;
        var actionName = action.ActionName;
        if (PublicActions.Contains($"{controller}.{actionName}"))
        {
            return;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var isAdmin = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId && role.Name == "Admin"
            select role.Id
        ).AnyAsync(context.HttpContext.RequestAborted);
        if (isAdmin)
        {
            return;
        }

        var allowed = await (
            from userRole in dbContext.UserRoles
            join permission in dbContext.RolePermissions on userRole.RoleId equals permission.RoleId
            where userRole.UserId == userId &&
                  permission.ControllerName == controller &&
                  permission.ActionName == actionName
            select permission.RoleId
        ).AnyAsync(context.HttpContext.RequestAborted);

        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }
}

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var createdUserRole = false;
        foreach (var roleName in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"Could not create the {roleName} role.");
                createdUserRole |= roleName == "User";
            }
        }

        if (createdUserRole)
        {
            var userRole = await roleManager.FindByNameAsync("User")
                ?? throw new InvalidOperationException("The default User role was not created.");
            db.RolePermissions.AddRange(
                new RolePermission { RoleId = userRole.Id, ControllerName = "Student", ActionName = "Index" },
                new RolePermission { RoleId = userRole.Id, ControllerName = "Faculty", ActionName = "Index" });
            await db.SaveChangesAsync();
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Configure SeedAdmin:Email and SeedAdmin:Password before running outside Development.");
            }
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, password);
            EnsureSucceeded(result, "Could not create the seed administrator.");
        }
        else if (!admin.EmailConfirmed)
        {
            admin.EmailConfirmed = true;
            EnsureSucceeded(await userManager.UpdateAsync(admin), "Could not confirm the seed administrator.");
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, "Admin"), "Could not assign the administrator role.");
        }

    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{message} {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}
