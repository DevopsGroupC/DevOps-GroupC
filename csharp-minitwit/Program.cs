using System.Reflection;

using csharp_minitwit;
using csharp_minitwit.Middlewares;
using csharp_minitwit.Services.Interfaces;
using csharp_minitwit.Services.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using Prometheus;

using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Initialize Serilog from appsettings.json configuration
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Dependency injection
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddDistributedMemoryCache();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// Export metrics from all HTTP clients registered in services
builder.Services.UseHttpClientMetrics();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".minitwit.Session";
    options.IdleTimeout = TimeSpan.FromSeconds(10000);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<MinitwitContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IFollowerRepository, FollowerRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMetaDataRepository, MetaDataRepository>();

builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Setup logging to console
builder.Logging.AddConsole();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MinitwitContext>();
    dbContext.Database.Migrate();
}

// Log statement to indicate the application is starting
Log.Information("Starting csharp-minitwit");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment() || !app.Environment.IsStaging())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseCookiePolicy();
app.MapMetrics(); // "/metrics"
app.UseHttpMetrics();
app.UseMiddleware<CatchAllMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{id?}/{action=Timeline}/");
app.MapControllers();

app.Run();
