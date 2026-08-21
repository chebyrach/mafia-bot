using MafiaBot;
using MafiaBot.Options;
using MafiaBot.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.Telegram));
builder.Services.AddScoped<TimerService>();

var host = builder.Build();
host.Run();