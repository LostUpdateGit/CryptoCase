using CryptoCase.Models;
using CryptoCase.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Qiwi.BillPayments.Model.Out;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace CryptoCase.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly MainService mainService;

        public PaymentController(MainService mainService)
        {
            this.mainService = mainService;
        }

        public static string CreateToken(string input, string k)
        {
            byte[] key = Encoding.UTF8.GetBytes(k);
            HMACSHA256 myhmacsha1 = new HMACSHA256(key);
            byte[] byteArray = Encoding.UTF8.GetBytes(input);
            MemoryStream stream = new MemoryStream(byteArray);
            return myhmacsha1.ComputeHash(stream).Aggregate("", (s, e) => s + String.Format("{0:x2}", e), s => s);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] QiwiResponse response)
        {
            BillResponse bill = response.Bill;
            StringValues hash;
            Request.Headers.TryGetValue("X-Api-Signature-SHA256", out hash);
            string invoice_parameters = $"{bill.Amount.CurrencyString}|{bill.Amount.ValueString}|{bill.BillId}|{bill.SiteId}|{bill.Status.ValueString}";
            string secret_key = "eyJ2ZXJzaW9uIjoiUDJQIiwiZGF0YSI6eyJwYXlpbl9tZXJjaGFudF9zaXRlX3VpZCI6ImRvb2c0aC0wMCIsInVzZXJfaWQiOiI3OTk2MDE4MTMyMiIsInNlY3JldCI6ImVjMjMwYjQ2Y2RmNWMwNGEyZGExMzg1MjY2MjFkYmExMmEyMGQyZTgzY2QyZTA4NThkMWY0OTAzMDM3NjRjNWEifX0=";
            string token = CreateToken(invoice_parameters, secret_key);

            List<string> comment = bill.Comment.Split('|').ToList();
            long chatId = Convert.ToInt64(comment[0]);
            string username = comment[1];

            if (token.ToLower() != hash.ToString().ToLower())
                return new OkObjectResult(await mainService.telegramClient.SendTextMessageAsync(chatId, $"Ошибка! Не совпадает контрольная сумма."));

            await mainService.telegramClient.SendTextMessageAsync(chatId, $"Оплата прошла успешно!");

            Case Case = mainService.casesPendingPayment.FirstOrDefault(x => x.chatId == chatId);
            if (Case == null)
                return new OkObjectResult(await mainService.telegramClient.SendTextMessageAsync(chatId, $"Фаил не найден среди создающихся"));

            Case.paid = true;
            if(Case.ready == false)
                return new OkObjectResult(await mainService.telegramClient.SendTextMessageAsync(chatId, "Фаил еще не готов. Ожидание может занять более 2 минут..."));

            await mainService.SendCase(chatId, username, Case);
            return Ok();
        }
    }
}
