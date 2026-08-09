using MafiaBot;
using MafiaBot.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.Telegram));

var host = builder.Build();
host.Run();