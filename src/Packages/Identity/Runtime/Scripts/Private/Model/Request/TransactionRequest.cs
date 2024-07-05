using System;
using System.Collections.Generic;

namespace Portal.Identity.Model
{
    [Serializable]
    public class TransactionRequest
    {
        public int ChainId { get; set; }
        public string ContractId { get; set; }
        public string PolicyId { get; set; }
        public string FunctionName { get; set; }
        public List<string> FunctionArgs { get; set; }

        public TransactionRequest()
        {
            FunctionArgs = new List<string>();
        }
    }
}
