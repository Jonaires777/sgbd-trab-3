using transaction_manager.Models;
using transaction_manager.Operations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = Directory.GetParent(exeDir)!.Parent!.Parent!.Parent!.FullName;
        string resultsDir = Path.Combine(projectRoot, "Results");
        Directory.CreateDirectory(resultsDir);

        var lines = File.ReadAllLines(Path.Combine(projectRoot, "in.txt"));
        var dataItems = lines[0].TrimEnd(';').Split(',').Select(s => s.Trim()).ToList();
        var transactions = lines[1].TrimEnd(';').Split(',').Select(s => s.Trim()).ToList();
        var timestamps = lines[2].TrimEnd(';').Split(',').Select(int.Parse).ToList();

        var transactionTimestamps = new Dictionary<int, int>();
        for (int i = 0; i < transactions.Count; i++)
            transactionTimestamps[i + 1] = timestamps[i];

        var output = new List<string>();

        var dataTS = new Dictionary<string, DataItemInfo>();
        foreach (var item in dataItems)
            dataTS[item] = new DataItemInfo();

        for (int i = 3; i < lines.Length; i++)
        {
            string line = lines[i];
            string scheduleId = line.Substring(0, line.IndexOf('-'));
            string scheduleRaw = line.Substring(line.IndexOf('-') + 1);

            var ops = ParseOperations.Parse(scheduleRaw);
            bool rollback = false;
            var moment = 0;

            foreach (var op in ops)
            {
                op.Moment = moment++;
                if (op.Type == "c") continue;

                string log = $"{scheduleId}-{(op.Type == "r" ? "Read" : "Write")}-{op.Moment}";
                string logFile = Path.Combine(resultsDir, $"{op.DataItem}.txt");
                File.AppendAllText(logFile, log + Environment.NewLine);

                int ts = transactionTimestamps[op.TransactionId];
                var itemInfo = dataTS[op.DataItem];

                if (op.Type == "r")
                {
                    if (ts < itemInfo.WriteTS)
                    {
                        output.Add($"{scheduleId}-ROLLBACK-{op.Moment}");
                        rollback = true;
                        break;
                    }
                    else
                    {
                        if (itemInfo.ReadTS < ts)
                            itemInfo.ReadTS = ts;
                    }
                }
                else if (op.Type == "w")
                {
                    if (ts < itemInfo.ReadTS || ts < itemInfo.WriteTS)
                    {
                        output.Add($"{scheduleId}-ROLLBACK-{op.Moment}");
                        rollback = true;
                        break;
                    }
                    else
                    {
                        itemInfo.WriteTS = ts;
                    }
                }
            }

            if (!rollback)
                output.Add($"{scheduleId}-OK");
        }

        File.WriteAllLines(Path.Combine(resultsDir, "out.txt"), output);
    }
}
