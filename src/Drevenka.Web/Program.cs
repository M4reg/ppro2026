using Drevenka.Infrastructure;
using Drevenka.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Načtení connection stringu
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Připojovací řetězec 'DefaultConnection' nebyl nalezen v konfiguraci.");

// Registrace infrastruktury (EF Core, PostgreSQL, Repozitáře, Služby)
builder.Services.AddInfrastructure(connectionString);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

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
