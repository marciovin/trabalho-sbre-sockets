using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var host = args.Length > 0 ? args[0] : "127.0.0.1";
        int port = args.Length > 1 ? int.Parse(args[1]) : 5000;

        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        Console.WriteLine($"Conectado a {host}:{port}");

        using var ns = client.GetStream();
        using var reader = new StreamReader(ns, Encoding.UTF8);
        using var writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };

        Console.WriteLine("Comandos: GET /mensagem | ECHO <txt> | TIME | PING");
        Console.WriteLine("Ctrl+C para sair.");

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // ---- PING com medição de RTT ----
            if (line.Trim().Equals("PING", StringComparison.OrdinalIgnoreCase))
            {
                var sw = Stopwatch.StartNew();
                await writer.WriteLineAsync("PING");
                var resp = await reader.ReadLineAsync();
                sw.Stop();
                Console.WriteLine($"Servidor: {resp} | RTT ~ {sw.ElapsedMilliseconds} ms");
                continue;
            }

            // ---- SPEEDTEST (mede throughput do socket) ----
            if (line.StartsWith("SPEEDTEST", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync(line);
                var header = await reader.ReadLineAsync(); // "200 START <bytes>" ou erro 400
                if (header == null)
                {
                    Console.WriteLine("Sem resposta do servidor.");
                    continue;
                }

                var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3 && parts[0] == "200" && parts[1] == "START" && long.TryParse(parts[2], out long total))
                {
                    byte[] buffer = new byte[8192];
                    long received = 0;
                    var sw = Stopwatch.StartNew();

                    while (received < total)
                    {
                        int toRead = (int)Math.Min(buffer.Length, total - received);
                        int n = await ns.ReadAsync(buffer, 0, toRead);
                        if (n <= 0) break; // conexão terminou?
                        received += n;
                    }

                    sw.Stop();

                    double seconds = Math.Max(1e-9, sw.Elapsed.TotalSeconds);
                    double mbps = (received * 8d) / 1_000_000d / seconds;
                    double MBps = (received / 1_000_000d) / seconds;

                    Console.WriteLine($"Recebidos {received / 1_000_000d:F2} MB em {seconds:F2} s  →  {mbps:F2} Mbps ({MBps:F2} MB/s)");
                }
                else
                {
                    Console.WriteLine($"Servidor: {header}");
                }
                continue;
            }

            // Demais comandos simples (linha→linha)
            await writer.WriteLineAsync(line);
            var response = await reader.ReadLineAsync();
            Console.WriteLine($"Servidor: {response}");
        }
    }
}
