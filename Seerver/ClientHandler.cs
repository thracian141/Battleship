using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Project
{
    public class ClientHandler
    {
        private TcpClient client;
        private NetworkStream stream;

        public ClientHandler(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
        }

        public void StartHandling()
        {
            // Start a new thread to handle client communication
            Console.WriteLine("Handling client communication...");

            // Receive and send messages in a loop
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Received: " + receivedMessage);

                    // Process the received message if needed

                    // Echo the message back to the client
                    byte[] responseBytes = Encoding.UTF8.GetBytes(receivedMessage);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    break;
                }
            }

            Console.WriteLine("Client disconnected.");
            stream.Close();
            client.Close();
        }
    }

}
