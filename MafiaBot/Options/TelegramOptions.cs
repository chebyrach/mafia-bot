using System;
using System.Collections.Generic;
using System.Text;

namespace MafiaBot.Options
{
    public class TelegramOptions
    {
        public string Token { get; set; } = string.Empty;
        public const string Telegram = nameof(Telegram);
    }
}
