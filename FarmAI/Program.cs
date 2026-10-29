using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FarmAI.Data;
using FarmAI.Models;
using FarmAI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder=WebApplication.CreateBuilder(args);
var provider=builder.Configuration["FarmAI:DbProvider"]??"SqlServer";
builder.Services.AddDbContext<FarmDbContext>(o=>
{
    if(string.Equals(provider,"Sqlite",StringComparison.OrdinalIgnoreCase))o.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")??"Data Source=farm-ai.db");
    else o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")??throw new Exception("Missing SQL Server connection"),s=>s.CommandTimeout(30));
});
builder.Services.AddScoped<AccessScope>();
builder.Services.AddScoped<FarmService>();
builder.Services.AddScoped<OperationsService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ChatContextService>();
builder.Services.AddScoped<ChatModelClient>();
builder.Services.AddScoped<OpenAiReportClient>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddHostedService<AlertWorker>();
builder.Services.AddHttpClient("ollama",c=>c.Timeout=TimeSpan.FromSeconds(25));
builder.Services.AddHttpClient("farm-chat",c=>c.Timeout=TimeSpan.FromSeconds(32));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>
{
    o.LoginPath="/Account/Login";o.AccessDeniedPath="/Account/Denied";
    o.Cookie.Name="FarmAI.Session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;
    o.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan=TimeSpan.FromMinutes(15);o.SlidingExpiration=false;
    o.Events.OnValidatePrincipal=async context=>
    {
        var db=context.HttpContext.RequestServices.GetRequiredService<FarmDbContext>();
        var sid=Guid.TryParse(context.Principal?.FindFirstValue("session"),out var value)?value:Guid.Empty;
        var s=await db.Sessions.FindAsync(sid);
        var u=s==null?null:await db.Users.FindAsync(s.UserId);
        if(u==null||!u.Active||s==null||s.RevokedAt!=null||s.ExpiresAt<=DateTime.UtcNow||
            context.Principal?.FindFirstValue(ClaimTypes.Role)!=u.Role)
        {
            context.RejectPrincipal();await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews(o=>
{
    o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    o.Filters.Add(new BusinessExceptionFilter());
});
builder.Services.AddRateLimiter(o=>
{
    o.RejectionStatusCode=429;
    o.AddPolicy("login",ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>
        new FixedWindowRateLimiterOptions{PermitLimit=8,Window=TimeSpan.FromMinutes(15),QueueLimit=0,AutoReplenishment=true}));
    o.AddPolicy("ai",ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)??ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>
        new FixedWindowRateLimiterOptions{PermitLimit=8,Window=TimeSpan.FromHours(1),QueueLimit=0,AutoReplenishment=true}));
});
var app=builder.Build();
CultureInfo.DefaultThreadCurrentCulture=CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture=new CultureInfo("vi-VN"); // HTML number inputs submit decimals with dot.
if(!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");app.UseHsts();
}
app.UseHttpsRedirection();app.UseStaticFiles();app.UseRouting();app.UseAuthentication();app.UseAuthorization();app.UseRateLimiter();
app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}");
await DemoSeed.InitializeAsync(app.Services,app.Configuration,app.Environment);
app.Run();
