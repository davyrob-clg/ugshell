using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CommandLine;

namespace UGShellExecute
{

    class UGShell

    {

        static void Main(string[] args)
        {

            int port = 23;  // Default port 
            string host = "localhost";

            Boolean launchServer = true;
            // Simple loop to check arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--server":
                        // Check if there is a name following the flag

                        launchServer = true;
                        continue;

                    case "--client":
                        // Check if there is a name following the flag

                        launchServer = false;

                        continue;

                    case "--version":
                        Console.WriteLine("v1.0.0");
                        continue;

                    case "--port":
                        // Check if there is a name following the flag


                        port = Convert.ToInt32(args[++i]);
                        Console.WriteLine($"Port selected: {port}");
                        continue;

                    case "--host":
                        // Check if there is a name following the flag


                        host = args[++i];
                        Console.WriteLine($"Host selected: {host}");

                        continue;

                    case "--help":
                        Console.WriteLine("Usage: ugshell --server | --client | --version | --help");
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {args[i]}");
                        break;
                }


            }
            if (launchServer)
            {
                Console.WriteLine($"UniGib Shell - Server started!");
                TelnetServer server = new TelnetServer(port);

                server.MainServer().Wait();
            }
            else
            {

                Console.WriteLine($"UniGib Shell - Client Started!");
                TelnetClient client = new TelnetClient(host, port);
                client.MainClient();  // Run the client 
            }
        }
    }
}
