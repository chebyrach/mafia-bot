namespace MafiaBot;
using MafiaBot.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class Worker : BackgroundService
{
    private readonly TelegramOptions _telegramOptions;
    private readonly ILogger<Worker> _logger;
    public Worker(IOptions<TelegramOptions> telegramOptions, ILogger<Worker> logger)
    {
        _telegramOptions = telegramOptions.Value;
        _logger = logger;
        Console.WriteLine("Hello, World!");
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bot = new TelegramBotClient(_telegramOptions.Token);
        var me = await bot.GetMe();
        bot.OnError += OnError;
        bot.OnMessage += OnMessage;
        bot.OnUpdate += OnUpdate;
        async Task OnError(Exception exception, HandleErrorSource source)
        {
            Console.WriteLine(exception);
        }

        async Task OnMessage(Message msg, UpdateType type)
        {
            _logger.LogInformation($"Message received: {msg.From}: {msg.Text}");
            if (msg.Text == "/start")
            {
                await bot.SendMessage(msg.Chat, "Welcome! Pick one direction",
                    replyMarkup: new InlineKeyboardButton[] { "Left", "Right" });
            }
        }

        async Task OnUpdate(Update update)
        {
            if (update is { CallbackQuery: { } query })
            {
                await bot.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");
                await bot.SendMessage(query.Message!.Chat, $"User {query.From} clicked on {query.Data}");
            }
        }
    }
}
