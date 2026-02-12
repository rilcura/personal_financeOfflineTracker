using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Jobs;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;
using PersonalFinanceOfflineTracker.Infrastructure.Services;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FinanceDb")
    ?? "Data Source=personal_finance.db";
builder.Services.AddInfrastructureSqlite(connectionString);
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));

builder.Services.AddSingleton<ITelegramCommandParser, TelegramCommandParser>();
builder.Services.AddScoped<ITelegramIngestService, DbTelegramIngestService>();
builder.Services.AddHttpClient<TelegramBotApiUpdateSource>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
    client.BaseAddress = new Uri(options.ApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.LongPollTimeoutSeconds + 10);
});
builder.Services.AddSingleton<InMemoryTelegramUpdateSource>();
builder.Services.AddSingleton<ITelegramUpdateSource>(sp =>
{
    var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BotToken))
    {
        return sp.GetRequiredService<InMemoryTelegramUpdateSource>();
    }

    return sp.GetRequiredService<TelegramBotApiUpdateSource>();
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
