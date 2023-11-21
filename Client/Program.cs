using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            StartClient();
        }

        static void StartClient()
        {
            try
            {
                int port = 12345; // Use the same port as the server
                string serverIp = "127.0.0.1"; // Use the server's IP address

                TcpClient client = new TcpClient(serverIp, port);
                NetworkStream stream = client.GetStream();

                Console.WriteLine("Connected to the server.");

                while (true)
                {
                    Console.Write("Enter a message (or 'exit' to quit): ");
                    string message = Console.ReadLine();

                    if (message.ToLower() == "exit")
                        break;

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    stream.Write(messageBytes, 0, messageBytes.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine("Server response: " + response);
                }

                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
    }
}
