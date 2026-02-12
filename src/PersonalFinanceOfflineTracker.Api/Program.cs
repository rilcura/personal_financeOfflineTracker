using System.Text.Json.Serialization;
using PersonalFinanceOfflineTracker.Api.Endpoints;
using PersonalFinanceOfflineTracker.Sync.Abstractions;
using PersonalFinanceOfflineTracker.Sync.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<ISyncService, InMemorySyncService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapSyncEndpoints();

app.Run();
