using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoCase.Models
{
    public class Pair
    {
        public string Base { get; set; }
        public string Quote { get; set; }
        public decimal Price { get; set; }
    }

    public class Asset
    {
        public string Symbol { get; set; }
        public string Pair { get; set; }
        public decimal AvrPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal CurPrice { get; set; }
        public decimal CurUsdtPrice { get; set; }
        public decimal CurValue { get; set; }
        public decimal CurUsdtValue { get; set; }
        public decimal Profit { get; set; }
        public decimal PercentProfit { get; set; }
        public decimal Share { get; set; }
    }

    public class Case
    {
        public long chatId { get; set; }
        public bool paid { get; set; }
        public bool ready { get; set; }
        public TimeSpan timeInWork { get; set; }

        public List<Pair> Pairs = new List<Pair>();
        public List<Asset> Assets = new List<Asset>();
        public decimal TotalValue = 0;
        public decimal TotalProfit = 0;
        public decimal TotalPercentProfit = 0;
    }
}
