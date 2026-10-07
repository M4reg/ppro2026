using Drevenka.Infrastructure;
using Drevenka.Infrastructure.Data;

using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// Nastavení české kultury (měna Kč, formát čísel a dat)
var czechCulture = new CultureInfo("cs-CZ");
czechCulture.NumberFormat.CurrencySymbol = "Kč";
czechCulture.NumberFormat.CurrencyPositivePattern = 3; // n $ -> 120,00 Kč
czechCulture.NumberFormat.CurrencyNegativePattern = 8; // -n $ -> -120,00 Kč
CultureInfo.DefaultThreadCurrentCulture = czechCulture;
CultureInfo.DefaultThreadCurrentUICulture = czechCulture;

// Načtení connection stringu
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Připojovací řetězec 'DefaultConnection' nebyl nalezen v konfiguraci.");

// Registrace infrastruktury (EF Core, PostgreSQL, Repozitáře, Služby)
builder.Services.AddInfrastructure(connectionString);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(czechCulture),
    SupportedCultures = new[] { czechCulture },
    SupportedUICultures = new[] { czechCulture }
});

// Automatická aplikace migrací a inicializace databáze
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<DrevenkaDbContext>();
        await DatabaseSeeder.InitializeDatabaseAsync(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chyba při spuštění databázových migrací a seedování dat.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
