using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

class TelnetServer
{
    private string host= "127.0.0.1"; 
    private int port=23;  // Default port


    // Constructor for server
    public TelnetServer(int port)
    {
        //this.host = host;
        this.port = port; 
    }
    public async Task MainServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, this.port);
        listener.Start();
        Console.WriteLine($"Telnet Server started on port: {this.port}...");

        while (true)
        {
            // Accept multiple concurrent clients using Task-based async
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    public async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            await WriteLine(stream, "Welcome to the C# Telnet Server!");
            await WriteLine(stream, "Type 'help' for commands or 'exit' to quit.");

            byte[] buffer = new byte[1024];
            while (true)
            {
                await Write(stream, "\r\n> "); // Prompt
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string input = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                string[] words = input.Split(' ');

                if (words.Length == 0) {

                    Console.WriteLine("Command not received");
                    continue;
                }

                string response = "Command not found";
                switch (words[0].ToLower())
                {
                    case "help":
                        Console.WriteLine("You selected help.");
                        response = "Available commands: time, status, help, exit";
                        break;
                    case "dir":
                        Console.WriteLine("You selected dir.");
                        await RunProcessAndPipeOutput("dir", stream);
                        response = "completed";
                        break;
                    case "cd":
                        Console.WriteLine("You selected dir.");
                        await RunProcessAndPipeOutput(input, stream);
                        response = "completed";
                        break;
                    case "pwd":
                        Console.WriteLine("You selected dir.");
                        await RunProcessAndPipeOutput("pwd", stream);
                        response = "completed";
                        break;



                    default:
                        Console.WriteLine("Command not found");
                        break;
                }

                /*
                // Process individual commands
                string response = input.ToLower() switch
                {
                    "help" => "Available commands: time, status, help, exit",
                    
                    "time" => $"Current Server Time: {DateTime.Now}",
                    
                    "status" => "Server is running smoothly.",

                    // Run the command and pipe output back to the stream
                    "dir" => await RunProcessAndPipeOutput("dir", stream),
                    
                    "exit" => "Goodbye!",
                    _ => $"Unknown command: {input}"
                };
                */

                await WriteLine(stream, response);
                if (input.ToLower() == "exit") break;
            }
        }
    }
    public  async Task RunProcessAndPipeOutput(string command, NetworkStream stream)
    {
        using Process process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            RedirectStandardOutput = true, // Required to capture output
            RedirectStandardError = true,
            UseShellExecute = false,      // Required for redirection
            CreateNoWindow = true
        };

        // Event handler to capture and send live output
        process.OutputDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                await WriteLine(stream, e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine(); // Start async reading

        await process.WaitForExitAsync();
        await WriteLine(stream, $"--- Process Exited with Code {process.ExitCode} ---");
    }

    public async Task Write(NetworkStream stream, string text)
    {
        byte[] data = Encoding.ASCII.GetBytes(text);
        await stream.WriteAsync(data, 0, data.Length);
    }

    public async Task WriteLine(NetworkStream stream, string text)
    {
        await Write(stream, text + "\r\n");
    }
}
