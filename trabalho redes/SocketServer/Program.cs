using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        int port = 5000;
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Servidor escutando na porta {port}...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client); // atende múltiplos clientes em paralelo
        }
    }

    // ================= CLIENTE =================
    static async Task HandleClientAsync(TcpClient client)
    {
        var remote = client.Client.RemoteEndPoint?.ToString();
        Console.WriteLine($"Conexão de {remote}");

        using var ns = client.GetStream();
        using var reader = new StreamReader(ns, Encoding.UTF8);
        using var writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };

        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine($"Recebido de {remote}: {line}");
                
                // Processa os outros comandos
                var response = ProcessCommand(line);
                await writer.WriteLineAsync(response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro com {remote}: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine($"Desconectado: {remote}");
        }
    }

    // ================= COMANDOS =================
    static string ProcessCommand(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return "400 Empty command";

        var parts = cmd.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var method = parts[0].ToUpperInvariant();
        var arg = parts.Length > 1 ? parts[1] : "";

        switch (method)
        {
            case "GET" when arg == "/mensagem":
                return "200 " + JsonSerializer.Serialize(new { mensagem = "Oi do servidor!" });

            case "ECHO":
                return "200 " + arg;

            case "TIME":
                return "200 " + DateTime.UtcNow.ToString("o");

            case "PING":
                return "200 PONG";

            default:
                return "400 Unknown command";
        }
    }
}