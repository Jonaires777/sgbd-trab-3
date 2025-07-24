using transaction_manager.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace transaction_manager.Operations
{
    public static class ParseOperations
    {
        public static List<Operation> Parse(string schedule, List<string> transactionNames)
        {
            var operations = new List<Operation>();
            // Regex melhorada para capturar melhor as operações
            var matches = Regex.Matches(schedule, @"([rw])(\d+)\(([A-Z])\)|c(\d*)");

            foreach (Match match in matches)
            {
                if (match.Value.StartsWith("c"))
                {
                    operations.Add(new Operation
                    {
                        Type = "c",
                        DataItem = string.Empty,
                        TransactionName = "" // pode ser ignorado
                    });
                }
                else
                {
                    var type = match.Groups[1].Value;
                    var transactionId = int.Parse(match.Groups[2].Value);
                    var dataItem = match.Groups[3].Value;

                    // Verificar se o transactionId está dentro do range válido
                    if (transactionId <= transactionNames.Count)
                    {
                        string transactionName = transactionNames[transactionId - 1];

                        operations.Add(new Operation
                        {
                            Type = type,
                            DataItem = dataItem,
                            TransactionName = transactionName
                        });
                    }
                }
            }
            return operations;
        }
    }
}