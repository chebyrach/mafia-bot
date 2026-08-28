namespace MafiaBot;

using MafiaBot.Options;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class Worker : BackgroundService 
{
    private readonly TelegramOptions _telegramOptions;
    private readonly ILogger<Worker> _logger;
    private readonly Services.TimerService _timerService;
    public Worker(IOptions<TelegramOptions> telegramOptions, ILogger<Worker> logger, Services.TimerService timer)
    {
        _telegramOptions = telegramOptions.Value;
        _logger = logger;
        _timerService = timer;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bot = new TelegramBotClient(_telegramOptions.Token);
        var me = await bot.GetMe(cancellationToken: stoppingToken);
        var players = new List<User>();
        var pollPlayers = new List<long>();
        var pollSelection = new Dictionary<long, int>();
        Models.Game game = new([1, 1, 1]);
        bot.OnError += OnError;
        bot.OnMessage += OnMessage;
        bot.OnUpdate += OnUpdate;
        

        async Task OnMessage(Message msg, UpdateType type)
        {
            if (string.IsNullOrEmpty(msg.Text)) return;
            if (msg.Text.StartsWith($"/start_game @{me.Username}", StringComparison.OrdinalIgnoreCase))
            {
                if (msg.From == null) throw new ArgumentException("Данные были пустые или null", nameof(msg.From));
                players.Add(msg.From);
                long gameId = msg.From.Id;
                var userKeyboard = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("Присоединиться", $"join_game_{gameId}") }
                });
                var creatorKeyboard = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("Начать игру", $"start_game_{gameId}|chat_{msg.Chat.Id}") }
                });
                try
                {
                    await bot.SendMessage(
                        chatId: msg.Chat.Id,
                        text: $"Игра №{gameId}",
                        replyMarkup: userKeyboard,
                        cancellationToken: default
                    );

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
            _logger.LogInformation($"{query.From.ToString()}, {query.Message}, {query.Data}");
            if (string.IsNullOrEmpty(query.Data)) throw new ArgumentException("Данные были пустые или null", nameof(query.Data));
            var data = query.Data;
            var user = query.From;
            if (data.StartsWith("start_game_"))
            {
                if (players.Count < 3)
                {
                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: "Мало игроков",
                        cancellationToken: default
                    ); 
                    return;
                }
                else if (players.Count > 15)
                {

                    await bot.AnswerCallbackQuery(
                        callbackQueryId: query.Id,
                        text: "Много игроков",
                        cancellationToken: default
                    );
                    return;
                }

                if (string.IsNullOrEmpty(data)) throw new ArgumentException("Входные данные пустые или null", nameof(data));
                string cleanData = data.Replace("start_game_", "");
                string[] parts = cleanData.Split('|');
                if (parts.Length < 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
                {
                    throw new ArgumentException("Неверные данные. Ожидалось: start_game_gameId|chatId");
                }

                string gameId = parts[0];
                string chatId = parts[1].Replace("chat_", "");

                try {     
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
                game = new Models.Game(players.Select(x => x.Id).ToList());
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await GameStart(chatId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка в игровом цикле");
                    }
                });
                }
                catch{ return; }
            }
            else if (data.StartsWith("join_game_"))
            {
                string gameId = data.Replace("join_game_", "");
                if (string.IsNullOrEmpty(gameId)) throw new ArgumentException("Данные были пустые или null", nameof(gameId));
                if (players.Any(p => p.Id == user.Id))
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
                    if (pollSelection.TryGetValue(targetId, out int currentVotes))
                    {
                        if (currentVotes == players.Count)
                        {
                            game.KickPlayer(targetId);
                            _timerService.StopTimer(targetId);
                        }
                        pollSelection[targetId] = currentVotes + 1;
                    }
                    else
                        pollSelection.Add(targetId, 1);
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
                _timerService.StopTimer(targetId);
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
                game.DetectiveWalks(targetId);
                _timerService.StopTimer(targetId);
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
                if (pollSelection.TryGetValue(targetId, out int currentVotes))
                {
                    if (currentVotes == players.Count)
                    {
                        game.MafiaWalks(targetId);
                        _timerService.StopTimer(targetId);
                    }
                    pollSelection[targetId] = currentVotes + 1;
                }
                else
                    pollSelection.Add(targetId, 1);
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
        async Task<Message> SendMessage(ChatId chatId, string text)
        {
            return await bot.SendMessage(
                chatId: chatId,
                text: text,
                cancellationToken: default
                );
        }
        async Task<Message> SendMessageWithKeyboard(ChatId chatId, string text, ReplyMarkup inlineKeyboard)
        {
            return await bot.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: inlineKeyboard,
                cancellationToken: default
                );
        }

        async Task GameStart(string chatId)
        {
            var civillianButtons = new List<InlineKeyboardButton>();
            var doctorButtons = new List<InlineKeyboardButton>();
            var detectiveButtons = new List<InlineKeyboardButton>();
            var mafiaButtons = new List<InlineKeyboardButton>();
            var roundNumber = 0;
            while (!game.CheckForCivilianWin() && !game.CheckForMafiaWin())
            {
                foreach (var player in players)
                {
                    civillianButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"kick_{player.Id}"));
                    if (game.GetListForDoctor().Contains(player.Id))
                        doctorButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"heal_{player.Id}"));
                    if (game.GetListForDetective().Contains(player.Id))
                        detectiveButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"check_{player.Id}"));
                    mafiaButtons.Add(InlineKeyboardButton.WithCallbackData($"{player.Username}", $"kill_{player.Id}"));
                }
                var civillianKeyboard = new InlineKeyboardMarkup(civillianButtons);
                var doctorKeyboard = new InlineKeyboardMarkup(doctorButtons);
                var detectiveKeyboard = new InlineKeyboardMarkup(detectiveButtons);
                var mafiaKeyboard = new InlineKeyboardMarkup(mafiaButtons);
                if (roundNumber == 0)
                {
                    await bot.SendMessage(
                        chatId: chatId,
                        text: "Наступает ознакомительная ночь, перейдите в чат с ботом",
                        cancellationToken: default
                    );
                    if (game.GetDetective() is long detectiveId)
                    {
                        await bot.SendMessage(
                        chatId: detectiveId,
                        text: "Вы детектив",
                        cancellationToken: default
                        );
                    }
                    if (game.GetDoctor() is long doctorId)
                    {
                        await bot.SendMessage(
                        chatId: doctorId,
                        text: "Вы доктор",
                        cancellationToken: default
                    );
                    }
                    if (game.GetMafia() is List<long> mafia)
                    {
                        foreach (var killer in mafia)
                        {
                            var teammatesId = mafia.Where(x => x != killer);
                            var teammatesUsernames = teammatesId
                              .Select(id => players.FirstOrDefault(x => x.Id == id)?.Username)
                              .Where(username => username != null);
                            string team = string.Join(", @", teammatesUsernames);
                            await bot.SendMessage(
                                chatId: killer,
                                text: string.IsNullOrEmpty(team) ? "Вы мафия" : $"Вы мафия. Ваши напарники {team}",
                                cancellationToken: default
                            );
                        }
                    }
                    if (game.GetCivilians() is List<long> civilians)
                    {
                        foreach (var civillian in civilians)
                        {
                            await bot.SendMessage(
                                chatId: civillian,
                                text: "Вы мирный житель",
                                cancellationToken: default
                            );
                        }
                    }
                    await bot.SendMessage(
                        chatId: chatId,
                        text: "Наступил день",
                        cancellationToken: default
                    );
                    await _timerService.StartTimer(TimeSpan.FromSeconds(15));
                    roundNumber++;
                }
                else
                {
                    if (roundNumber > 2)
                    {
                        await bot.SendMessage(
                            chatId: chatId,
                            text: "Выгнать игрока",
                            replyMarkup: civillianKeyboard,
                            cancellationToken: default
                        );
                     game.KickPlayer(calcPollResults(await _timerService.StartTimer(TimeSpan.FromSeconds(10) ) ) );

                    }
                     await bot.SendMessage(
                        chatId: chatId,
                        text: "Наступает ночь, перейдите в чат с ботом",
                        cancellationToken: default
                    );
                    await Task.Delay(1000);
                    if (game.GetDetective() is long detectiveId)
                    {
                        await bot.SendMessage(
                            chatId: detectiveId,
                            text: "Проверить",
                            replyMarkup: detectiveKeyboard,
                            cancellationToken: default
                        );
                        if (await _timerService.StartTimer(TimeSpan.FromSeconds(10)) == null)
                        {
                            game.DetectiveWalks(players.FirstOrDefault().Id);
                        }
                    }
                    await Task.Delay(1000);

                    if (game.GetMafia() is List<long> mafia)
                    {
                        foreach (var killer in mafia)
                        {
                            await bot.SendMessage(
                                chatId: killer,
                                text: "Убить",
                                replyMarkup: mafiaKeyboard,
                                cancellationToken: default
                            );
                        }
                        game.MafiaWalks(calcPollResults(await _timerService.StartTimer(TimeSpan.FromSeconds(10))));
                    }
                    await Task.Delay(1000);

                    if (game.GetDoctor() is long doctorId)
                    {
                        await bot.SendMessage(
                            chatId: doctorId,
                            text: "Вылечить",
                            replyMarkup: doctorKeyboard,
                            cancellationToken: default
                        );
                        if ( await _timerService.StartTimer(TimeSpan.FromSeconds(15)) == null )
                        {
                            game.DoctorWalks(players.FirstOrDefault().Id);
                        }
                    }
                    roundNumber++;
                    await bot.SendMessage(
                        chatId: chatId,
                        text: "Наступил день",
                        cancellationToken: default
                    );
                    await Task.Delay(10000, default);
                }
                if (roundNumber != 1)
                {
                    if (game.CheckRoundResults() is long killedPlayer)
                    {
#pragma warning disable CS8602
                        await bot.SendMessage(
                            chatId: chatId,
                            text: $"@{players.FirstOrDefault(x => x.Id.Equals(killedPlayer)).Username} был убит",
                            cancellationToken: default
                        );
#pragma warning restore CS8602
                        players.RemoveAll(x => x.Id.Equals(killedPlayer));
                    }
                }
                detectiveButtons.Clear();
                mafiaButtons.Clear();
                civillianButtons.Clear();
                doctorButtons.Clear();
            }
        }
        long calcPollResults(long? @targetId)
        {
            long pollResult = 0;
            if (pollSelection.Count != 0)
            {
                int result = 0;
                foreach (var player in pollSelection)
                {
                    if (result == 0)
                        result = player.Value;
                    else
                    {
                        result = player.Value > result ? player.Value : result;
                        pollResult = player.Key;
                    }
                }
            }
            if (pollResult == 0)
            {
#pragma warning disable CS8602

                pollPlayers.Clear();
                pollSelection.Clear();
                return players.FirstOrDefault().Id;

#pragma warning restore CS8602
            }
            else
            {
                pollPlayers.Clear();
                pollSelection.Clear();
                return pollResult;
            }
        } 
    }
}