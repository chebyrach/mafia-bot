namespace MafiaBot;

using MafiaBot.Models;
using MafiaBot.Options;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
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

        async Task OnMessage(Message msg, UpdateType type)
        {
            if (string.IsNullOrEmpty(msg.Text))
                return;
            if (msg.Text.StartsWith($"/start_game @{me.Username}", StringComparison.OrdinalIgnoreCase))
            {
                long gameId = msg.From.Id;
                var userKeyboard = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithUrl("", $"https://t.me/{me.Username}?start=game_{gameId}")}
                });
                var creatorKeyboard = new InlineKeyboardMarkup(new[] {
                    new[] { InlineKeyboardButton.WithCallbackData("Начать игру", $"start_game_{gameId}") }
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
        async Task OnJoinChat(Update update)
        {

        }
        async Task OnCallBackQuery(CallbackQuery query) {
            var data = query.Data;
            if (data.StartsWith("/start_game_"))
            {
                Match match = Regex.Match(data, @"/start_game_\d+");
                    
            }
        }
        async Task OnError(Exception exception, HandleErrorSource source)
        {
            _logger.LogInformation($"Exception: {exception}, source {source}");
        }
    }
}