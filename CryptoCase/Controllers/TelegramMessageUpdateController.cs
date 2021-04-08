using Binance.Net;
using CryptoCase.Models;
using CryptoCase.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoCase.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramMessageUpdateController : ControllerBase
    {
        private readonly MainService mainService;
        private readonly BinanceService binanceService;

        public TelegramMessageUpdateController(MainService mainService, BinanceService binanceService)
        {
            this.mainService = mainService;
            this.binanceService = binanceService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return new OkObjectResult("ReturnText");
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (Regex.IsMatch(update.Message.Text, "[A-z0-9]{64} [A-z0-9]{64}"))
            {
                MatchCollection matchList = Regex.Matches(update.Message.Text, "[A-z0-9]{64}");
                List<string> list = matchList.Cast<Match>().Select(match => match.Value).ToList();
                BinanceClient binanceClient = binanceService.GetClient(list[0], list[1]);
                if (binanceClient == null)
                    return new OkObjectResult(await mainService.telegramClient.SendTextMessageAsync(update.Message.From.Id, "Аккаунт Binance с указанной парой ключ/секрет не найден"));

                //Если аккаунт найден
                InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] {
                        new[] { new InlineKeyboardButton() { Text = "Оплатить через QIWI", Url = mainService.CreatePaymentURL(update.Message.From.Id, update.Message.From.Username).AbsoluteUri } }
                    });
                await mainService.telegramClient.SendTextMessageAsync(update.Message.From.Id, "Стоймость услуги 35 руб.", replyMarkup: keyboard);

                //Если фаил для этого пользователя еще не существует
                if (mainService.casesPendingPayment.FirstOrDefault(x => x.chatId != update.Message.From.Id) == null)
                {
                    try
                    {
                        Thread sendCaseThread = new Thread(new ParameterizedThreadStart(binanceService.CreateCase));
                        sendCaseThread.Start(new CreateCaseParameters() { Client = binanceClient, ChatId = update.Message.From.Id, Username = update.Message.From.Username });
                    } catch
                    {
                        await mainService.telegramClient.SendTextMessageAsync(update.Message.From.Id, "Не удалось создать поток!");
                    }
                }
            }
            else if(update.Message.Text.ToLower() == "/пример" || update.Message.Text.ToLower() == "/example")
            {
                await mainService.SendExample(update.Message.From.Id);
            }
            else
            {
                await mainService.telegramClient.SendTextMessageAsync(update.Message.From.Id, "Неверный формат. Правильный вариант:\n<key> <secret>");
            }
            return Ok();
        }
    }
}
