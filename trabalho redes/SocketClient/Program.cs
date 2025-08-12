// Program.cs (Cliente)
using System;
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

        Console.WriteLine("Digite comandos (ex: GET /mensagem, ECHO oi). Ctrl+C para sair.");
        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line)) continue;
            await writer.WriteLineAsync(line);
            var response = await reader.ReadLineAsync();
            Console.WriteLine($"Servidor: {response}");
        }
    }
}
