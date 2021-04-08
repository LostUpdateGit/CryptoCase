using Qiwi.BillPayments.Model.Out;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoCase.Models
{
    public class QiwiResponse
    {
        public BillResponse Bill { get; set; }
        public string Version { get; set; }
    }
}
