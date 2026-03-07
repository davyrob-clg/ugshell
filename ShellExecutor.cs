using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace UGShellExecute
{


    public class ShellExecutor
    {
        public async Task<string> ExecuteAndStreamAsync(string command, string workingDir, NetworkStream tcpStream)
        {
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string fileName = isWindows ? "cmd.exe" : "/bin/bash";
            string arguments = isWindows ? $"/c \"{command}\"" : $"-c \"{command}\"";

            Console.WriteLine($"Execute: Working Dir is: {workingDir}");

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDir,
                //WorkingDirectory = "C:\\Users\\daver\\git\\ugshell",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using var process = new Process { StartInfo = startInfo };
            try
            {

                process.Start();
                // Capture both streams into one StringBuilder for the return value
                var capturedOutput = new StringBuilder();

                // Task-based "Tee" to pipe to TCP and capture as string simultaneously
                Task outputTask = TeeStreamAsync(process.StandardOutput.BaseStream, tcpStream, capturedOutput);
                Task errorTask = TeeStreamAsync(process.StandardError.BaseStream, tcpStream, capturedOutput);

                await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync());

                Console.WriteLine($"Execute: Captured Data is {capturedOutput.ToString()}");

                return capturedOutput.ToString();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return $"Error starting process: {ex.Message}";
            }
            
        }

        private async Task TeeStreamAsync(Stream source, Stream destination, StringBuilder capture)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            // Read from process stream until it closes
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                // 1. Write to TCP Stream
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead));

                // 2. Convert to string and append to capture buffer
                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                capture.Append(chunk);
            }
        }
    }


}