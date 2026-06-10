using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SmartCampus.Data;
using SmartCampus.Models;
using SmartCampus.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<UserStoreService>();
builder.Services.AddSingleton<FacultyAcademicService>();
builder.Services.AddSingleton<CourseRegistrationRequestService>();
builder.Services.AddSingleton<CampusRealtimeService>();

builder.Services.AddScoped<AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/signin", async (HttpContext ctx, UserStoreService store) =>
{
    if (!ctx.Request.HasFormContentType)
        return Results.Redirect("/account/login?error=invalid");

    var form = await ctx.Request.ReadFormAsync();
    var email = (form["email"].FirstOrDefault() ?? "").Trim();
    var password = form["password"].FirstOrDefault() ?? "";
    var remember = form["remember"].FirstOrDefault() == "true";
    var selectedRole = form["selectedrole"].FirstOrDefault() ?? "";
    var facultyMode = form["facultymode"].FirstOrDefault() ?? "Teacher";

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        return Results.Redirect($"/account/login?error=invalid&role={selectedRole}&fmode={facultyMode}");

    if (!store.ValidateCredentials(email, password))
        return Results.Redirect($"/account/login?error=invalid&role={selectedRole}&fmode={facultyMode}");

    var user = store.FindByEmail(email)!;

    var roleMismatch = user.Role switch
    {
        UserRole.Admin => selectedRole != "Admin",
        UserRole.HOD => selectedRole != "HOD",
        UserRole.Faculty => selectedRole != "Faculty",
        UserRole.Student => selectedRole != "Student",
        _ => true
    };

    if (roleMismatch)
        return Results.Redirect($"/account/login?error=invalid&role={selectedRole}&fmode={facultyMode}");

    var initials = string.Concat(
        user.FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w[0]));

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name,  user.Email),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role,  user.Role.ToString()),
        new Claim("FullName",    user.FullName),
        new Claim("UserId",      user.StudentId),
        new Claim("Department",  user.Department),
        new Claim("Designation", user.Designation),
        new Claim("Initials",    initials),
        new Claim("FacultyMode", facultyMode)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = remember,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

    var destination = user.Role switch
    {
        UserRole.Admin => "/dashboard/admin",
        UserRole.HOD => "/dashboard/hod",
        UserRole.Faculty => "/dashboard/faculty",
        UserRole.Student => "/dashboard/home",
        _ => "/account/login"
    };

    return Results.Redirect(destination);
})
.DisableAntiforgery();

app.MapGet("/auth/signin", () => Results.Redirect("/account/login"));

app.MapGet("/auth/signout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/account/login");
});

app.MapRazorComponents<SmartCampus.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/", () => Results.Redirect("/account/login"));

app.Run();