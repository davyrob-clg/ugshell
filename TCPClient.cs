using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UGShellExecute
{

    class TelnetClient
    {
        private string host = "127.0.0.1";
        private int port = 23;  // Default port

        // Constructor for server
        public TelnetClient(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        public TelnetClient()
        {

        }
        public void MainClient()
        {


            try
            {
                using TcpClient client = new TcpClient(host, port);
                using NetworkStream stream = client.GetStream();
                Console.WriteLine($"Connected to {host}:{port}");

                // Start a thread to read responses continuously
                Thread readThread = new Thread(() => ReadFromServer(stream));
                readThread.IsBackground = true;
                readThread.Start();

                // Main loop to send individual commands
                while (true)
                {
                    string command = Console.ReadLine();
                    if (string.IsNullOrEmpty(command)) continue;
                    if (command.ToLower() == "exit") break;

                    // Send command with a newline (Telnet style)
                    byte[] data = Encoding.ASCII.GetBytes(command + "\r\n");
                    stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void ReadFromServer(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.Write(response);
                    }
                }
                catch { break; }
            }
        }
    }
}