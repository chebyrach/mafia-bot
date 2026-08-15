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
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bot = new TelegramBotClient(_telegramOptions.Token);
        var me = await bot.GetMe();
        var players = new List<string>();
        bot.OnError += OnError;
        bot.OnMessage += OnMessage;
        bot.OnUpdate += OnUpdate;
        

        async Task OnMessage(Message msg, UpdateType type)
        {
            if (string.IsNullOrEmpty(msg.Text)) throw new ArgumentException("Данные были пустые или null", nameof(msg.Text));
            if (msg.Text.StartsWith($"/start_game @{me.Username}", StringComparison.OrdinalIgnoreCase))
            {
                if (msg.From == null) throw new ArgumentException("Данные были пустые или null", nameof(msg.From));
                players.Add(msg.From.Id.ToString());
                long gameId = msg.From.Id;
                var userKeyboard = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData($"join_game_{gameId}") }
                });
                var creatorKeyboard = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("Начать игру", $"start_game_{gameId}|chat_{msg.Chat.Id}") }
                });

                await bot.SendMessage(
                    chatId: msg.Chat.Id,
                    text: $"Присоединиться",
                    replyMarkup: userKeyboard
                );

                try
                {
                    await bot.SendMessage(
                        chatId: gameId,
                        text: "Нажмите, когда все присоединятся для запуска игры",
                        replyMarkup: creatorKeyboard
                    );
                    
                }
                catch
                {
                    await bot.SendMessage(
                        chatId: msg.Chat.Id,
                        text: $"@{msg.From.Username} для управления игрой, запустите личный чат с мной"
                    );
                }
            }
            else if (msg.Text.Contains($"@{me.Username}"))
                await bot.SendMessage(
                    chatId: msg.Chat.Id,
                    text: $"Для запуска игры напишите в чат /start_game @{me.Username}"
                );
        }

        async Task OnUpdate(Update update)
        {   
            switch (update.Type)
            {
                case UpdateType.Message:
                {
                    if (update.Message != null)
                        await OnMessage(update.Message, UpdateType.Message);
                    break;
                }
                case UpdateType.CallbackQuery:
                {
                    if (update.CallbackQuery != null)
                        await OnCallBackQuery(update.CallbackQuery);
                    break;
                }
            }
        }
        //async Task OnJoinGroupChat(Update update)
        //{

        //}
        //async Task OnJoinPrivateChat(Update update)
        //{

        //}
        async Task OnCallBackQuery(CallbackQuery query) {
            if (string.IsNullOrEmpty(query.Data)) throw new ArgumentException("Данные были пустые или null", nameof(query.Data));
            var data = query.Data;
            if (data.StartsWith("start_game_"))
            {
                string gameId = data.Replace("start_game_", "");
                if (string.IsNullOrEmpty(gameId)) throw new ArgumentException("Данные были пустые или null", nameof(gameId));
                string chatId = data.Substring(data.IndexOf('|') + 1, gameId.Length);
                if (string.IsNullOrEmpty(chatId)) throw new ArgumentException("Данные были пустые или null", nameof(chatId));
                await bot.AnswerCallbackQuery(
                    callbackQueryId: query.Id, 
                    text: "Игра запускается..."
                );
                await bot.SendMessage(
                    chatId: chatId,
                    text: $"Игра №{gameId} запускается"
                );
            }
            else if (data.StartsWith("join_game_"))
            {
                string gameId = data.Replace("join_game_", "");
                if (string.IsNullOrEmpty(gameId)) throw new ArgumentException("Данные были пустые или null", nameof(gameId));
                if (players.Contains(query.From.Id.ToString()))
                {
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: $"Вы уже присоединились к игре №{gameId}"
                    );
                    return;
                }
                else
                {
                    players.Add(query.From.Id.ToString());
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: $"Вы присоединились к игре №{gameId}"
                    );
                }
            }
        }
        async Task OnError(Exception exception, HandleErrorSource source)
        {
            _logger.LogInformation($"Exception: {exception}, source {source}");
        }
    }
}