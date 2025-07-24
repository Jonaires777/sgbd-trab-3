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

        var transactionTimestamps = new Dictionary<string, int>();
        for (int i = 0; i < transactions.Count; i++)
            transactionTimestamps[transactions[i]] = timestamps[i];

        var output = new List<string>();

        string debugFile = Path.Combine(resultsDir, "debug.txt");
        File.WriteAllText(debugFile, "=== DEBUG INICIADO ===" + Environment.NewLine);

        File.AppendAllText(debugFile, $"Linhas lidas: {lines.Length}" + Environment.NewLine);
        File.AppendAllText(debugFile, $"Data items: {string.Join(", ", dataItems)}" + Environment.NewLine);
        File.AppendAllText(debugFile, $"Transactions: {string.Join(", ", transactions)}" + Environment.NewLine);
        File.AppendAllText(debugFile, $"Timestamps: {string.Join(", ", timestamps)}" + Environment.NewLine);

        for (int i = 3; i < lines.Length; i++)
        {
            string line = lines[i];
            File.AppendAllText(debugFile, $"\n=== PROCESSANDO LINHA {i}: {line} ===" + Environment.NewLine);

            string scheduleId = line.Substring(0, line.IndexOf('-'));
            string scheduleRaw = line.Substring(line.IndexOf('-') + 1);

            File.AppendAllText(debugFile, $"Schedule ID: {scheduleId}" + Environment.NewLine);
            File.AppendAllText(debugFile, $"Schedule Raw: {scheduleRaw}" + Environment.NewLine);

            // IMPORTANTE: Reinicializar estrutura de dados para cada escalonamento
            var dataTS = new Dictionary<string, DataItemInfo>();
            foreach (var item in dataItems)
                dataTS[item] = new DataItemInfo();

            var ops = ParseOperations.Parse(scheduleRaw, transactions);
            File.AppendAllText(debugFile, $"Operações parseadas: {ops.Count}" + Environment.NewLine);

            foreach (var op in ops)
            {
                File.AppendAllText(debugFile, $"Op: {op.Type} - Transação: {op.TransactionName} - Dado: {op.DataItem}" + Environment.NewLine);
            }

            bool rollback = false;
            int localMoment = 0; // Momento local para cada escalonamento

            foreach (var op in ops)
            {
                if (op.Type == "c")
                {
                    // NOVA REGRA: Commit zera os timestamps de TODOS os dados
                    File.AppendAllText(debugFile, $"COMMIT detectado para {op.TransactionName} - Zerando timestamps de TODOS os dados" + Environment.NewLine);

                    // Zerar ReadTS e WriteTS de todos os dados
                    foreach (var item in dataItems)
                    {
                        int oldReadTS = dataTS[item].ReadTS;
                        int oldWriteTS = dataTS[item].WriteTS;

                        dataTS[item].ReadTS = 0;
                        dataTS[item].WriteTS = 0;

                        File.AppendAllText(debugFile, $"Dados {item}: ReadTS {oldReadTS} -> 0, WriteTS {oldWriteTS} -> 0" + Environment.NewLine);
                    }

                    // IMPORTANTE: Commit também conta no momento local
                    localMoment++;
                    continue;
                }

                op.Moment = localMoment++;

                // Debug: vamos imprimir o que está acontecendo
                string debugMsg = $"{scheduleId} - {op.Type}{transactionTimestamps[op.TransactionName]}({op.DataItem}) - Momento: {op.Moment}";
                Console.WriteLine(debugMsg);
                File.AppendAllText(debugFile, debugMsg + Environment.NewLine);

                string debugState = $"  Antes: ReadTS={dataTS[op.DataItem].ReadTS}, WriteTS={dataTS[op.DataItem].WriteTS}";
                Console.WriteLine(debugState);
                File.AppendAllText(debugFile, debugState + Environment.NewLine);

                // Salvar no arquivo do dado
                string logFile = Path.Combine(resultsDir, $"{op.DataItem}.txt");
                string log = $"{scheduleId},{(op.Type == "r" ? "read" : "write")},{op.Moment}";
                File.AppendAllText(logFile, log + Environment.NewLine);

                // Algoritmo TS-Básico CORRIGIDO com timestamp atual (pode ter sido reiniciado)
                int ts = transactionTimestamps[op.TransactionName];
                var itemInfo = dataTS[op.DataItem];

                if (op.Type == "r")
                {
                    // Se TS(Tx) < TS-Write(dado) → ROLLBACK
                    if (ts < itemInfo.WriteTS)
                    {
                        string rollbackMsg = $"  ROLLBACK: TS({op.TransactionName})={ts} < WriteTS({op.DataItem})={itemInfo.WriteTS}";
                        Console.WriteLine(rollbackMsg);
                        File.AppendAllText(debugFile, rollbackMsg + Environment.NewLine);
                        output.Add($"{scheduleId}-ROLLBACK-{op.Moment}");
                        rollback = true;
                        break;
                    }
                    // Senão, atualizar TS-Read(dado) = max(TS-Read(dado), TS(Tx))
                    itemInfo.ReadTS = Math.Max(itemInfo.ReadTS, ts);
                    string okMsg = $"  OK: ReadTS({op.DataItem}) atualizado para {itemInfo.ReadTS}";
                    Console.WriteLine(okMsg);
                    File.AppendAllText(debugFile, okMsg + Environment.NewLine);
                }
                else if (op.Type == "w")
                {
                    // Se TS(Tx) < TS-Read(dado) OU TS(Tx) < TS-Write(dado) → ROLLBACK
                    if (ts < itemInfo.ReadTS || ts < itemInfo.WriteTS)
                    {
                        string rollbackMsg = $"  ROLLBACK: TS({op.TransactionName})={ts} < ReadTS({op.DataItem})={itemInfo.ReadTS} OU < WriteTS({op.DataItem})={itemInfo.WriteTS}";
                        Console.WriteLine(rollbackMsg);
                        File.AppendAllText(debugFile, rollbackMsg + Environment.NewLine);
                        output.Add($"{scheduleId}-ROLLBACK-{op.Moment}");
                        rollback = true;
                        break;
                    }
                    // Senão, atualizar TS-Write(dado) = TS(Tx)
                    itemInfo.WriteTS = ts;
                    string okMsg = $"  OK: WriteTS({op.DataItem}) atualizado para {itemInfo.WriteTS}";
                    Console.WriteLine(okMsg);
                    File.AppendAllText(debugFile, okMsg + Environment.NewLine);
                }
            }

            if (!rollback)
                output.Add($"{scheduleId}-OK");
        }

        File.WriteAllLines(Path.Combine(resultsDir, "out.txt"), output);
    }
}