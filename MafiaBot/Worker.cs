namespace MafiaBot;

using MafiaBot.Options;
using Models;
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
        var players = new List<User>();
        var gamePlayers = new List<Player>();
        bot.OnError += OnError;
        bot.OnMessage += OnMessage;
        bot.OnUpdate += OnUpdate;
        

        async Task OnMessage(Message msg, UpdateType type)
        {
            if (string.IsNullOrEmpty(msg.Text)) throw new ArgumentException("Данные были пустые или null", nameof(msg.Text));
            if (msg.Text.StartsWith($"/start_game @{me.Username}", StringComparison.OrdinalIgnoreCase))
            {
                if (msg.From == null) throw new ArgumentException("Данные были пустые или null", nameof(msg.From));
                players.Add(msg.From);
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
                    replyMarkup: userKeyboard,
                    cancellationToken: default
                );

                try
                {
                    await bot.SendMessage(
                        chatId: gameId,
                        text: "Нажмите, когда все присоединятся для запуска игры",
                        replyMarkup: creatorKeyboard,
                        cancellationToken: default
                    );
                }
                catch
                {
                    await bot.SendMessage(
                        chatId: msg.Chat.Id,
                        text: $"@{msg.From.Username} для управления игрой, запустите личный чат с мной",
                        cancellationToken: default
                    );
                }
            }
            else if (msg.Text.Contains($"@{me.Username}"))
                await bot.SendMessage(
                    chatId: msg.Chat.Id,
                    text: $"Для запуска игры напишите в чат /start_game @{me.Username}",
                    cancellationToken: default
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
                    text: "Игра запускается...",
                    cancellationToken: default
                );
                await bot.SendMessage(
                    chatId: chatId,
                    text: $"Игра №{gameId} запускается",
                    cancellationToken: default
                );
            }
            else if (data.StartsWith("join_game_"))
            {
                string gameId = data.Replace("join_game_", "");
                if (string.IsNullOrEmpty(gameId)) throw new ArgumentException("Данные были пустые или null", nameof(gameId));
                if (players.Contains(query.From))
                {
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: $"Вы уже присоединились к игре №{gameId}",
                        cancellationToken: default
                    );
                    return;
                }
                else
                {
                    players.Add(query.From);
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: $"Вы присоединились к игре №{gameId}",
                        cancellationToken: default
                    );
                }
            }
        }
        async Task OnError(Exception exception, HandleErrorSource source)
        {
            _logger.LogInformation($"Exception: {exception}, source: {source}");
        }
        async Task GameStart()
        {
            var game = new Models.Game(players.Select(x => x.Id).ToList());
            var civillianButtons = new List<InlineKeyboardButton>();
            var doctorButtons = new List<InlineKeyboardButton>();
            var detectiveButtons = new List<InlineKeyboardButton>();
            var mafiaButtons = new List<InlineKeyboardButton>();
            foreach (var player in players)
            {
                civillianButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"kick_{player.Id}"));
                doctorButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"heal_{player.Id}"));
                detectiveButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"check_{player.Id}"));
                mafiaButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"kill_{player.Id}"));
            }
            var civillianKeyboard = new InlineKeyboardMarkup(civillianButtons);
            //var doctorKeyboard = new InlineKeyboardMarkup(new[] {
            //        new[] { InlineKeyboardButton.WithCallbackData("Начать игру", $"start_game_{1} |chat_ {1}") }
            //    });
            //var mafiaKeyboard = new InlineKeyboardMarkup(new[] {
            //        new[] { InlineKeyboardButton.WithCallbackData("Начать игру", $"start_game_{1}|chat_{1}") }
            //    });
            //var detectiveKeyboard = new InlineKeyboardMarkup(new[] {
            //        new[] { InlineKeyboardButton.WithCallbackData("Начать игру", $"start_game_{1}|chat_{1}") }
            //    });
        }
        async Task GameTimer(TimeProvider time, Task task)
        {
            
        }
    }
}