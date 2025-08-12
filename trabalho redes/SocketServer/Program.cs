// Program.cs (Servidor)
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
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
            _ = HandleClientAsync(client); // não aguarda: aceita múltiplos clientes
        }
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        var remote = client.Client.RemoteEndPoint?.ToString();
        Console.WriteLine($"Conexão de {remote}");
        using var ns = client.GetStream();
        using var reader = new StreamReader(ns, Encoding.UTF8);
        using var writer = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true };

    #nullable disable

    try
    {
      string line;
      while ((line = await reader.ReadLineAsync()) != null)
      {
        Console.WriteLine($"Recebido de {remote}: {line}");
        var response = ProcessCommand(line);
        await writer.WriteLineAsync(response); // envia resposta em uma linha
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

    static string ProcessCommand(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return "400 Empty command";

        var parts = cmd.Split(' ', 2);
        var method = parts[0].ToUpperInvariant();
        var arg = parts.Length > 1 ? parts[1] : "";

        if (method == "GET" && arg == "/mensagem")
        {
            var j = System.Text.Json.JsonSerializer.Serialize(new { mensagem = "Olá do servidor!" });
            return "200 " + j;
        }
        else if (method == "ECHO")
        {
            return "200 " + arg;
        }
        else if (method == "TIME")
        {
            return "200 " + DateTime.UtcNow.ToString("o");
        }
        return "400 Unknown command";
    }
}
