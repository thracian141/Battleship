using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Battleship;
using Newtonsoft.Json; // You need to have Newtonsoft.Json (JSON.NET) installed

namespace Project
{
    class Server
    {
        static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };
        static readonly string delimiter = "<EOF>";

        static void Main(string[] args) {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            int port = 12345; // Port number for the server
            //get this pc's IPv4
            IPAddress ipAddress = Array.FindLast(Dns.GetHostEntry(string.Empty).AddressList, 
                a => a.AddressFamily == AddressFamily.InterNetwork);
            Console.WriteLine(ipAddress);

            TcpListener listener = new TcpListener(ipAddress, port);
            listener.Start();

            Console.WriteLine("Server is listening for incoming connections...");

            TcpClient clientA = null;
            TcpClient clientB = null;

            while (true)
            {
                if (clientA == null)
                {
                    clientA = listener.AcceptTcpClient();
                    Console.WriteLine("Client A connected.");
                }
                else if (clientB == null)
                {
                    clientB = listener.AcceptTcpClient();
                    Console.WriteLine("Client B connected.");

                    // Perform the initialization step
                    await InitializeGame(clientA, clientB);
                }

                if (clientA != null && clientB != null)
                {
                    clientA.Close();
                    clientB.Close();
                    clientA = null;
                    clientB = null;
                }
            }
        }

        static async Task<int> InitializeGame(TcpClient clientA, TcpClient clientB)
        {
            Console.WriteLine("Started Initializing!");
            // Receive board and list for Client A
            int boardSize = await ReceiveIntData(clientA);
            Console.WriteLine("RECEIVED BOARD SIZE: " + boardSize);
            await SendIntData(clientB, boardSize);
            Console.WriteLine("Received and sent Board size!");

            ShipNode[,] gameBoardA = await ReceiveData<ShipNode[,]>(clientA);
            await SendData(clientB, gameBoardA);
            Console.WriteLine("Received and sent Board A!");

            ShipNode[,] gameBoardB = await ReceiveData<ShipNode[,]>(clientB);
            await SendData(clientA, gameBoardB);
            Console.WriteLine("Received and sent Board B!");

            return 0;
        }

        static async Task<int> ReceiveIntData(TcpClient client) {
            Console.WriteLine("RECEIVING");
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            await stream.ReadAsync(buffer, 0, buffer.Length);
            int data = BitConverter.ToInt32(buffer, 0);
            Console.WriteLine("RECEIVED INT");
            return data;
        }

        static async Task<int> SendIntData(TcpClient client, int data) {
            Console.WriteLine("SENDING");
            NetworkStream stream = client.GetStream();
            byte[] buffer = BitConverter.GetBytes(data);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            return 0;
        }

        static async Task<T> ReceiveData<T>(TcpClient client) {
            Console.WriteLine("RECEIVING");
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[8192];
            int bytesRead;
            Console.WriteLine("GOT CLIENT STREAM");

            using (MemoryStream ms = new MemoryStream()) {
                Console.WriteLine("USING MEMORY STREAM");
                Task<int> readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                if (await Task.WhenAny(readTask, Task.Delay(5000)) == readTask) {
                    // readTask completed within timeout
                    bytesRead = readTask.Result;
                    while (bytesRead > 0) {
                        Console.WriteLine("ENTERED WHILE LOOP");
                        ms.Write(buffer, 0, bytesRead);
                        readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                        if (await Task.WhenAny(readTask, Task.Delay(5000)) == readTask) {
                            // readTask completed within timeout
                            bytesRead = readTask.Result;
                        } else {
                            // timeout
                            break;
                        }
                    }
                } else {
                    // timeout
                    bytesRead = 0;
                }
                Console.WriteLine("EXITED WHILE LOOP");
                string json = Encoding.UTF8.GetString(ms.ToArray());
                var data = JsonConvert.DeserializeObject<T>(json, settings);
                Console.WriteLine("CREATED JSON");
                return data;
            }
        }

        static async Task<int> SendData<T>(TcpClient client, T data) {
            Console.WriteLine("SENDING");
            NetworkStream stream = client.GetStream();
            string json = JsonConvert.SerializeObject(data, settings);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            return 0;
        }
    }
}
