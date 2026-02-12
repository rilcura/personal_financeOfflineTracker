using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Jobs;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<InMemoryTelegramIngestStore>();
builder.Services.AddSingleton<ITelegramCommandParser, TelegramCommandParser>();
builder.Services.AddSingleton<ITelegramIngestService, TelegramIngestService>();
builder.Services.AddSingleton<ITelegramUpdateSource, InMemoryTelegramUpdateSource>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
