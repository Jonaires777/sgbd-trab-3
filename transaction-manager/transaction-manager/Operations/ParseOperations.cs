using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using transaction_manager.Models;

namespace transaction_manager.Operations
{
    public class ParseOperations
    {
        public static List<Operation> Parse(string raw)
        {
            var ops = new List<Operation>();
            var regex = new Regex(@"([rw|c])(\d)(?:\(([A-Z])\))?");
            foreach (Match match in regex.Matches(raw))
            {
                ops.Add(new Operation
                {
                    Type = match.Groups[1].Value,
                    TransactionId = int.Parse(match.Groups[2].Value),
                    DataItem = match.Groups[3].Success ? match.Groups[3].Value : ""
                });
            }
            return ops;
        }
    }
}
