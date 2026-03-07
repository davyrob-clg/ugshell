using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UGShellExecute
{

    class TelnetServer
    {
        private string host = "127.0.0.1";
        private int port = 23;  // Default port
        private string startDir;




        // Constructor for server
        public TelnetServer(int port)
        {
            //this.host = host;
            this.port = port;
            this.startDir = Environment.CurrentDirectory;
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
                await WriteLine(stream, "Welcome to UniGib the C# TCP Remote Server!");
                await WriteLine(stream, "Type 'help' for commands or 'exit' to quit.");

                ShellExecutor shellExecutor = new ShellExecutor();
                string pwd = this.startDir;  // Default PWD for the session, can be updated with 'cd' command

                byte[] buffer = new byte[1024];
                while (true)
                {
                    string response = "Command not found";
                    

                    await Write(stream, "\r\n> "); // Prompt
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string input = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                    string[] words = input.Split(' ');
                     
                    if (words.Length == 0)
                    {

                        Console.WriteLine("Command not received");
                        continue;
                    }

                    for (int i = 0; i < words.Length; i++)
                    {

                        Console.WriteLine($"Commands: {words[i]}");
                    }

                    switch (words[0].ToLower())
                    {
                        case "help":
                            Console.WriteLine("You selected help.");
                            response = "Available commands: time, status, help, exit";
                            break;
                        case "dir":
                            Console.WriteLine("You selected dir.");
                            //response = await RunProcessAndPipeOutput("dir", stream);

                            response = await shellExecutor.ExecuteAndStreamAsync("dir", pwd, stream);

                            Console.WriteLine($"Response: {response}");

                            break;
                        case "cd":

                            if (words.Length == 1)
                            {
                                response = "Usage: cd <directory>";
                                break;
                            }
                            

                            string cdreturn = $"cd {words[1]} && chdir";
                            Console.WriteLine($"cd command is: {cdreturn}");
                            

                            pwd = await shellExecutor.ExecuteAndStreamAsync(cdreturn, pwd, stream);

                            // Strip of that EOL - doesnt like it for some reason, and it causes issues with the next command if not removed
                            pwd = pwd.Replace("\r\n", string.Empty);


                            //response = await RunProcessAndPipeOutput(ucdreturn, stream);

                            Console.WriteLine($"current pwd of client connection is now: {pwd}");
                            break;


                       
                        case "pwd":

                            Console.WriteLine($"Current PWD {pwd}");

                            pwd = await shellExecutor.ExecuteAndStreamAsync("chdir", pwd, stream);

                            // Strip of that EOL - doesnt like it for some reason, and it causes issues with the next command if not removed
                            pwd = pwd.Replace("\r\n", string.Empty);

                            Console.WriteLine($"server response: {pwd}");
                            break;


                        default:

                            Console.WriteLine("Not found command.");
                            //response = await RunProcessAndPipeOutput(words[0], stream);
                            await WriteLine(stream, "Not found...");
                            

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

                    //await WriteLine(stream, response);
                    if (input.ToLower() == "exit") break;
                }
            }
        }
        public async Task<string> RunProcessAndPipeOutput(string command, NetworkStream stream)
        {
            using Process process = new Process();
            string response = "";
            string eresponse = "";

            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",

                //FileName = "cmd.exe",
                //Arguments = "/c \"(cd /users;pwd)\"",

                RedirectStandardOutput = true, // Required to capture output
                RedirectStandardError = false,
                UseShellExecute = false,      // Required for redirection
                CreateNoWindow = true
            };

            // Event handler to capture and send live output
            process.OutputDataReceived += async (sender, e) =>
            {
                Console.WriteLine($"{e.ToString()}");
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Write this back to the client stream
                    await WriteLine(stream, e.Data);
                    // Also add this to the response variable to return at the end for the class instance 
                    response += e.Data;

                }

            };

            // Event handler to capture and send live output
            process.ErrorDataReceived += async (sender, e) =>
            {
                Console.WriteLine($"{e.ToString()}");
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Write this back to the client stream
                    await WriteLine(stream, e.Data);
                    // Also add this to the response variable to return at the end for the class instance 
                    eresponse += e.Data;

                }

            };

            process.Start();
            process.BeginOutputReadLine(); // Start async reading

            await process.WaitForExitAsync();
            //await WriteLine(stream, $"--- Process Exited with Code {process.ExitCode} ---");
            Console.WriteLine($"Response from Task: {response} ");
            return response;
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
}