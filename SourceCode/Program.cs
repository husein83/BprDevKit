using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BprDevKit.Components;
using BprDevKit.Config;
using BprDevKit.DatabaseManagement;
using BprDevKit.DatabaseManagement.Connections;
using BprDevKit.DatabaseManagement.Decryption;
using BprDevKit.DatabaseManagement.SqlGenerators;
using BprDevKit.DataModelGenerator.Services;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);

// Read server port & startup configuration from appsettings
var configuredUrls = builder.Configuration["Urls"] ?? builder.Configuration["ASPNETCORE_URLS"];
var port = builder.Configuration.GetValue<int?>("ServerSettings:Port") ?? 5000;
var host = builder.Configuration.GetValue<string>("ServerSettings:Host") ?? "localhost";
var useHttps = builder.Configuration.GetValue<bool>("ServerSettings:UseHttps", false);
var openBrowser = builder.Configuration.GetValue<bool>("ServerSettings:OpenBrowserOnStartup", true);

if (string.IsNullOrWhiteSpace(configuredUrls))
{
    var scheme = useHttps ? "https" : "http";
    builder.WebHost.UseUrls($"{scheme}://{host}:{port}");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register configuration and services
builder.Services.AddSingleton<ServerConfiguration>();
builder.Services.AddSingleton<BprDevKit.Localization.ITranslationFactory, BprDevKit.Localization.TranslationFactory>();
builder.Services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
builder.Services.AddSingleton<ISqlGeneratorFactory, SqlGeneratorFactory>();
builder.Services.AddSingleton<SqlServerGenerator>();
builder.Services.AddSingleton<ISqlGenerator, SqlServerGenerator>();
builder.Services.AddSingleton<ISqlObjectDecryptor, SqlObjectDecryptor>();
builder.Services.AddSingleton<BprDevKit.Utility.Logging.IBprLogger>(BprDevKit.Utility.Logging.BprLogger.Instance);
builder.Services.AddScoped<DatabaseManager>();
builder.Services.AddSingleton<DatabaseManagerFactory>();
builder.Services.AddSingleton<DataModelGeneratorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (useHttps)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

ApplicationConfiguration.LoadConfig();

app.MapPost("/api/theme/{theme}", (string theme) =>
{
    if (theme == "dark" || theme == "light")
    {
        ApplicationConfiguration.Config.Theme = theme;
        ApplicationConfiguration.SaveChanges(ApplicationConfiguration.Config);
    }
    return Results.Ok();
}).DisableAntiforgery();

if (openBrowser)
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Task.Run(async () =>
        {
            try
            {
                // Small delay to ensure Kestrel is fully bound and accepting requests
                await Task.Delay(350);

                var serverAddresses = app.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>();

                var address = serverAddresses?.Addresses.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(address))
                {
                    var scheme = useHttps ? "https" : "http";
                    var browseHost = host == "0.0.0.0" || host == "*" || host == "+" ? "localhost" : host;
                    address = $"{scheme}://{browseHost}:{port}";
                }
                else
                {
                    address = address.Replace("0.0.0.0", "localhost")
                                     .Replace("+", "localhost")
                                     .Replace("*", "localhost");
                }

                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = address,
                        UseShellExecute = true
                    });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", address);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", address);
                }
            }
            catch
            {
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        var scheme = useHttps ? "https" : "http";
                        var browseHost = host == "0.0.0.0" || host == "*" || host == "+" ? "localhost" : host;
                        var fallbackAddress = $"{scheme}://{browseHost}:{port}";
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {fallbackAddress}") { CreateNoWindow = true });
                    }
                    catch { }
                }
            }
        });
    });
}

app.Run();