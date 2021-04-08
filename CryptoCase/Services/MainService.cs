using Binance.Net;
using ClosedXML.Excel;
using CryptoCase.Models;
using Microsoft.AspNetCore.Hosting;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Model;
using Qiwi.BillPayments.Model.In;
using Qiwi.BillPayments.Model.Out;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace CryptoCase.Services
{
    public class MainService
    {
        public TelegramBotClient telegramClient { get; set; }
        public List<Case> casesPendingPayment = new List<Case>();
        public List<Request> RequestHistory = new List<Request>();

        private readonly IWebHostEnvironment appEnvironment;
        private BillPaymentsClient qiwiClient;

        public MainService(IWebHostEnvironment appEnvironment)
        {
            this.appEnvironment = appEnvironment;
            telegramClient = new TelegramBotClient("1669889973:AAFJ-s9KHXY2UASJFuCVeaRE3FxDzpsOBZ0");
            qiwiClient = BillPaymentsClientFactory.Create(
                secretKey: "eyJ2ZXJzaW9uIjoiUDJQIiwiZGF0YSI6eyJwYXlpbl9tZXJjaGFudF9zaXRlX3VpZCI6ImRvb2c0aC0wMCIsInVzZXJfaWQiOiI3OTk2MDE4MTMyMiIsInNlY3JldCI6ImVjMjMwYjQ2Y2RmNWMwNGEyZGExMzg1MjY2MjFkYmExMmEyMGQyZTgzY2QyZTA4NThkMWY0OTAzMDM3NjRjNWEifX0="
            );
        }

        public async Task SendExample(long ChatId)
        {
            try
            {
                byte[] bytesJpg = System.IO.File.ReadAllBytes(Path.Combine(appEnvironment.ContentRootPath + "/wwwroot", $"Example.jpg"));
                using (var stream = new MemoryStream(bytesJpg))
                    await telegramClient.SendDocumentAsync(ChatId, new InputOnlineFile(stream, "Example.jpg"));

                byte[] bytesXlsx = System.IO.File.ReadAllBytes(Path.Combine(appEnvironment.ContentRootPath + "/wwwroot", $"Example.xlsx"));
                using (var stream = new MemoryStream(bytesXlsx))
                    await telegramClient.SendDocumentAsync(ChatId, new InputOnlineFile(stream, "Example.xlsx"));
            } catch
            {
                await telegramClient.SendTextMessageAsync(ChatId, "Не удалось отправить пример.");
            }
            
        }

        public async Task SendCase(long chatId, string username, Case Case)
        {
            if(Case.paid == false)
            {
                await telegramClient.SendTextMessageAsync(chatId, "Фаил готов и ожидает оплаты!");
            }
            else
            {
                byte[] bytes = await CreateExcelFileStream(Case);
                casesPendingPayment.Remove(Case);
                using (var stream = new MemoryStream(bytes))
                {
                    await telegramClient.SendDocumentAsync(chatId, new InputOnlineFile(stream, "Portfolio.xlsx"));
                }
                RequestHistory.Add(new Request() { ChatId = chatId, Username = username, DateTime = DateTime.Now, TimeInWork = Case.timeInWork });
            }  
        }

        public Uri CreatePaymentURL(int chatId, string username)
        {
            decimal sum = chatId != 526655661 ? 35.0m : 1.0m;
            BillResponse response = qiwiClient.CreateBill(
                new CreateBillInfo
                {
                    BillId = Guid.NewGuid().ToString(),
                    Amount = new MoneyAmount
                    {
                        ValueDecimal = sum,
                        CurrencyEnum = CurrencyEnum.Rub
                    },
                    Comment = $"{chatId}|{username}",
                    ExpirationDateTime = DateTime.Now.AddHours(1)
                }
            );
            return response.PayUrl;
        }

        public async Task<byte[]> CreateExcelFileStream(Case Case)
        {
            string path = Path.Combine(appEnvironment.ContentRootPath + "/wwwroot", "Template.xlsx");
            XLWorkbook workbook;
            try
            {
                workbook = new XLWorkbook(path);
            }
            catch
            {
                await telegramClient.SendTextMessageAsync(Case.chatId, "Не найден Excel шаблон");
                return null;
            }

            var worksheet = workbook.Worksheet(1);

            var rowCounter = 2;
            foreach (var asset in Case.Assets)
            {
                worksheet.Cell($"A{rowCounter}").Value = $"{asset.Symbol}/{asset.Pair}";
                worksheet.Cell($"B{rowCounter}").Value = RoundNum(asset.AvrPrice);
                worksheet.Cell($"C{rowCounter}").Value = asset.Pair;
                worksheet.Cell($"D{rowCounter}").Value = RoundNum(asset.Quantity);
                worksheet.Cell($"E{rowCounter}").Value = RoundNum(asset.CurPrice);
                worksheet.Cell($"F{rowCounter}").Value = asset.Pair;
                worksheet.Cell($"G{rowCounter}").Value = RoundNum(asset.CurValue);
                worksheet.Cell($"H{rowCounter}").Value = asset.Pair;
                worksheet.Cell($"I{rowCounter}").Value = RoundNum(asset.Profit);
                worksheet.Cell($"J{rowCounter}").Value = asset.Pair;
                worksheet.Cell($"K{rowCounter}").Value = asset.PercentProfit.ToString("P");
                worksheet.Cell($"L{rowCounter}").Value = asset.Share.ToString("P");

                IXLRangeAddress address = worksheet.Range(worksheet.Cell($"I{rowCounter}").Address, worksheet.Cell($"K{rowCounter}").Address).RangeAddress;
                worksheet.Range(address).Style.Font.FontColor = asset.Profit > 0 ? XLColor.FromHtml("#1D8348") : XLColor.FromHtml("#943126");
                rowCounter++;
            }
            worksheet.Columns().AdjustToContents();

            string savePath = Path.Combine(appEnvironment.ContentRootPath + "/wwwroot", $"{Guid.NewGuid().ToString()}.xlsx");
            workbook.SaveAs(savePath);
            var bytes = System.IO.File.ReadAllBytes(savePath);
            System.IO.File.Delete(savePath);
            return bytes;
        }

        string RoundNum(decimal num)
        {
            int roundSigns = (int)Math.Truncate(-Math.Log10((double)num)) + 3;
            roundSigns = roundSigns > 0 ? roundSigns : 2;
            return Math.Round(num, roundSigns).ToString($"N{roundSigns}");
        }
    }
}
