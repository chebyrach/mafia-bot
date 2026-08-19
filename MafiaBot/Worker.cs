namespace MafiaBot;

using MafiaBot.Options;
using Microsoft.Extensions.Options;
using Models;
using System.Runtime.InteropServices;
using System.Xml;
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
        var pollPlayers = new List<long>();
        var pollResult = new Dictionary<long, int>();
        var game = new Models.Game(players.Select(x => x.Id).ToList());
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
            var user = query.From;
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
                if (players.Contains(user))
                {
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: $"Вы уже присоединились к игре №{gameId}",
                        cancellationToken: default
                    );
                }
                else
                {
                    players.Add(user);
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: $"Вы присоединились к игре №{gameId}",
                        cancellationToken: default
                    );
                }
            }
            else if (data.StartsWith("kick_"))
            {
                if (pollPlayers.Contains(user.Id))
                {
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: $"Вы уже проголосовали",
                        cancellationToken: default
                    );
                }
                else
                {
                    pollPlayers.Add(user.Id);
                    _ = long.TryParse(data.Replace("kick_", ""), out long targetId);
                    if (pollResult.TryGetValue(targetId, out int currentVotes))
                        pollResult[targetId] = currentVotes + 1;
                    else
                        pollResult.Add(targetId, 1);
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: $"Вы проголосовали",
                        cancellationToken: default
                    );
                }
            }
            else if (data.StartsWith("heal_"))
            {
                _ = long.TryParse(data.Replace("heal_", ""), out long targetId);
                game.DoctorWalks(targetId:  targetId);
                await bot.AnswerCallbackQuery(
                    callbackQueryId: query.Id,
                    text: $"Вы вылечили игрока",
                    cancellationToken: default
                );
#pragma warning disable CS8602
                await bot.DeleteMessage(
                    chatId: user.Id,
                    messageId: query.Message.Id,
                    cancellationToken: default
                );
#pragma warning restore CS8602
            }
            else if (data.StartsWith("check_"))
            {
                _ = long.TryParse(data.Replace("check_", ""), out long targetId);
                await bot.SendMessage(
                    chatId: user.Id,
                    text: game.DetectiveWalks(targetId: targetId) ? "Игрок - мафия" : "Игрок -  мирный",
                    cancellationToken: default
                );
#pragma warning disable CS8602
                await bot.DeleteMessage(
                    chatId: user.Id,
                    messageId: query.Message.Id,
                    cancellationToken: default
                );
#pragma warning restore CS8602
            }
            else if (data.StartsWith("kill_"))
            {
                pollPlayers.Add(user.Id);
                _ = long.TryParse(data.Replace("kill_", ""), out long targetId);
                if (pollResult.TryGetValue(targetId, out int currentVotes))
                    pollResult[targetId] = currentVotes + 1;
                else
                    pollResult.Add(targetId, 1);
                await bot.AnswerCallbackQuery(
                    callbackQueryId: query.Id,
                    text: $"Вы проголосовали",
                    cancellationToken: default
                );
#pragma warning disable CS8602
                await bot.DeleteMessage(
                    chatId: user.Id,
                    messageId: query.Message.Id,
                    cancellationToken: default
                );
#pragma warning restore CS8602
            }
        }
        async Task OnError(Exception exception, HandleErrorSource source)
        {
            _logger.LogInformation($"Exception: {exception}, source: {source}");
        }
        async Task GameStart()
        {
            var civillianButtons = new List<InlineKeyboardButton>();
            var doctorButtons = new List<InlineKeyboardButton>();
            var detectiveButtons = new List<InlineKeyboardButton>();
            var mafiaButtons = new List<InlineKeyboardButton>();
            var roundNumber = 1;
            while (!game.CheckForCivilianWin() && !game.CheckForMafiaWin())
            {
                foreach (var player in players)
                {
                    civillianButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"kick_{player.Id}"));
                    if (game.GetListForDoctor().Contains(player.Id))
                        doctorButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"heal_{player.Id}"));
                    detectiveButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"check_{player.Id}"));
                    mafiaButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"kill_{player.Id}"));
                }
                var civillianKeyboard = new InlineKeyboardMarkup(civillianButtons);
                var doctorKeyboard = new InlineKeyboardMarkup(civillianButtons);
                var detectiveKeyboard = new InlineKeyboardMarkup(civillianButtons);
                var mafiaKeyboard = new InlineKeyboardMarkup(civillianButtons);
                if (roundNumber == 1)
                {
                }
                else
                {
                    await bot.SendMessage(
                        chatId: 1,
                        text: "Выгнать игрока",
                        replyMarkup: civillianKeyboard,
                        cancellationToken: default
                    );
                    await bot.SendMessage(
                        chatId: 1,
                        text: "",
                        cancellationToken: default
                    );
                    await bot.SendMessage(
                        chatId: game.GetDetective(),
                        text: "",
                        replyMarkup: detectiveKeyboard,
                        cancellationToken: default
                    );
                    roundNumber++;
                }
            }

        }
        async Task GameTimer(Timer time, Task task)
        {
            
        }
    }
}