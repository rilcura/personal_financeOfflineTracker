using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceOfflineTracker.Api.Endpoints;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;
using PersonalFinanceOfflineTracker.Infrastructure.Services;
using PersonalFinanceOfflineTracker.Sync.Abstractions;
using PersonalFinanceOfflineTracker.Sync.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FinanceDb")
    ?? "Data Source=personal_finance.db";

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddInfrastructureSqlite(connectionString);
builder.Services.AddSingleton<ISyncService, InMemorySyncService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.MapSyncEndpoints();

app.Run();
