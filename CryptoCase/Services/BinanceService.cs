using Binance.Net;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.SpotData;
using CryptoCase.Models;
using CryptoExchange.Net.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CryptoCase.Services
{
    public class BinanceService
    {
        private readonly MainService mainService;
        public BinanceService(MainService mainService)
        {
            this.mainService = mainService;
        }

        public BinanceClient GetClient(string key, string secret)
        {
            var binanceClient = new BinanceClient(new BinanceClientOptions {
                ApiCredentials = new ApiCredentials(key, secret)
            });

            if (binanceClient.General.GetAccountInfo().Data != null)
                return binanceClient;

            return null;
        }

        public void CreateCase(object param)
        {
            CreateCaseParameters caseParameters = (CreateCaseParameters)param;
            BinanceClient client = caseParameters.Client;

            Case Case = new Case();
            Case.chatId = caseParameters.ChatId;
            mainService.casesPendingPayment.Add(Case);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var data = client.General.GetAccountInfo().Data;
            List<BinanceBalance> balances;
            balances = data.Balances.Where(x => x.Total > 0 || x.Asset == "USDT").ToList();

            foreach (var bBase in balances)
                foreach (var bQuote in balances)
                    if (bBase.Asset != bQuote.Asset)
                    {
                        var webCallTrades = client.Spot.Order.GetMyTradesAsync(bBase.Asset + bQuote.Asset);
                        if (webCallTrades.Result.Data != null)
                        {
                            if (webCallTrades.Result.Data.Count() > 0)
                            {
                                decimal curPrice = client.Spot.Market.GetCurrentAvgPrice(bBase.Asset + bQuote.Asset).Data.Price;
                                Asset asset = new Asset()
                                {
                                    Symbol = bBase.Asset,
                                    Pair = bQuote.Asset,
                                    CurPrice = curPrice,
                                    CurUsdtPrice = bQuote.Asset != "USDT" ? client.Spot.Market.GetCurrentAvgPrice(bQuote.Asset + "USDT").Data.Price : curPrice
                                };

                                var tradesList = webCallTrades.Result.Data.ToList();
                                foreach (var trade in tradesList)
                                {
                                    asset.AvrPrice = trade.IsBuyer ? (asset.AvrPrice * asset.Quantity + trade.Price * trade.Quantity) / (asset.Quantity + trade.Quantity) : asset.AvrPrice;
                                    asset.Quantity += trade.IsBuyer ? trade.Quantity : -trade.Quantity;
                                    asset.Quantity = trade.CommissionAsset == asset.Symbol ? asset.Quantity - trade.Commission : asset.Quantity;
                                }

                                asset.CurValue = asset.CurPrice * asset.Quantity;
                                asset.CurUsdtValue = bQuote.Asset != "USDT" ? asset.CurValue * asset.CurUsdtPrice : asset.CurValue;
                                asset.Profit = asset.CurValue - asset.AvrPrice * asset.Quantity;
                                asset.PercentProfit = asset.CurPrice / asset.AvrPrice - 1;

                                Case.TotalValue += asset.CurUsdtValue;
                                Case.Assets.Add(asset);
                            }
                        }
                    }

            foreach (var asset in Case.Assets) {
                asset.Share = asset.CurUsdtValue / Case.TotalValue;
            }
            client.Dispose();

            stopwatch.Stop();
            Case.ready = true;
            Case.timeInWork = stopwatch.Elapsed;
            mainService.SendCase(caseParameters.ChatId, caseParameters.Username, Case).Wait();
        }
    }
}
