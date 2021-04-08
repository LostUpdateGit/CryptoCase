using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoCase.Models
{
    public class Request
    {
        public long ChatId { get; set; }
        public string Username { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan TimeInWork { get; set; }
    }
}
