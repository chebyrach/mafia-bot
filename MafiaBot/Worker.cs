namespace MafiaBot;
using MafiaBot.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class Worker : BackgroundService
{
    private readonly TelegramOptions _telegramOptions;
    private readonly ILogger<Worker> _logger;
    public Worker(IOptions<TelegramOptions> telegramOptions, ILogger<Worker> logger)
    {
        _telegramOptions = telegramOptions.Value;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bot = new TelegramBotClient(_telegramOptions.Token);
        var me = await bot.GetMe();
        var players = new List<string>();
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
            if (msg.Text is not { } text)
                Console.WriteLine($"Received a message of type {msg.Type}");
            else if (text.StartsWith('/'))
            {
                var command = new List<string>();
                command.AddRange(text.Split(" "));
                if (command != null && command.Count == 2)
                {
                    if (command[1].Equals($"@{me.Username}") && command[0].Equals("/start_game"))
                    {
                        players.Add(msg.From.Username);
                        var keyboard = new InlineKeyboardMarkup( new[]
                        { new[] { InlineKeyboardButton.WithCallbackData(text: "Записаться", callbackData: "join_game") } });
                        await bot.SendMessage(msg.Chat, "Игра создана",
                            replyMarkup: keyboard);
                    }
                    else if (command[1].Equals($"@{me.Username}") && command[0] != string.Empty)
                        await bot.SendMessage(msg.Chat, "Я не распознал команду");
                }
            }
            else if (text.Contains($"@{me.Username}"))
            {
                Console.Beep();
            }
        }

        async Task OnUpdate(Update update)
        {
            if (update is { CallbackQuery: { } query })
            {
                if (!players.Contains(query.From.Username))
                {
                    await bot.AnswerCallbackQuery(query.Id, $"Вы записались");
                    players.Add(query.From.Username);
                }
                else await bot.AnswerCallbackQuery(query.Id, $"Вы уже записаны");
            }
        }
    }
}