using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WrapPassword.Data;
using WrapPassword.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
var connectionStringBuilder = new SqliteConnectionStringBuilder(configuredConnectionString);

if (!Path.IsPathRooted(connectionStringBuilder.DataSource))
{
    connectionStringBuilder.DataSource = Path.Combine(
        builder.Environment.ContentRootPath,
        connectionStringBuilder.DataSource);
}

var databaseDirectory = Path.GetDirectoryName(connectionStringBuilder.DataSource)
    ?? throw new InvalidOperationException("The SQLite database directory could not be resolved.");
Directory.CreateDirectory(databaseDirectory);

builder.Services.AddProblemDetails();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionStringBuilder.ConnectionString));

var app = builder.Build();

await ApplicationDbInitializer.InitializeAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.MapStatusEndpoints();

app.Run();
