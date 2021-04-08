using Binance.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CryptoCase.Models
{
    public class CreateCaseParameters
    {
        public BinanceClient Client { get; set; }
        public long ChatId { get; set; }
        public string Username { get; set; }
    }
}
