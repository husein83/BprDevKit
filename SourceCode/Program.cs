using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BprDevKit.Components;
using BprDevKit.Config;
using BprDevKit.DatabaseManagement;
using BprDevKit.DatabaseManagement.Connections;
using BprDevKit.DatabaseManagement.Decryption;
using BprDevKit.DatabaseManagement.SqlGenerators;
using BprDevKit.DataModelGenerator.Services;

var builder = WebApplication.CreateBuilder(args);

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
app.UseHttpsRedirection();

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

app.Run();