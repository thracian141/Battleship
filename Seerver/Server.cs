using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Battleship;
using Newtonsoft.Json; // You need to have Newtonsoft.Json (JSON.NET) installed

namespace Project {
    class Server {
        static JsonSerializerSettings settings = new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        static void Main(string[] args) {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args) {
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

            while (true) {
                if (clientA == null) {
                    clientA = listener.AcceptTcpClient();
                    Console.WriteLine("Client A connected.");
                } else if (clientB == null) {
                    clientB = listener.AcceptTcpClient();
                    Console.WriteLine("Client B connected.");

                    // Perform the initialization step
                    await InitializeGame(clientA, clientB);
                }

                if (clientA != null && clientB != null) {
                    clientA.Close();
                    clientB.Close();
                    clientA = null;
                    clientB = null;
                }
            }
        }

        static async Task<int> InitializeGame(TcpClient clientA, TcpClient clientB) {
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
            StreamReader reader = new StreamReader(stream);
            Console.WriteLine("GOT CLIENT STREAM");

            StringBuilder sb = new StringBuilder();
            char[] buffer = new char[8192];
            int numRead;
            while ((numRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                string receivedData = new string(buffer, 0, numRead);
                int delimiterIndex = receivedData.IndexOf("\n\n"); // Assuming "\n\n" is the delimiter
                if (delimiterIndex >= 0) {
                    sb.Append(receivedData, 0, delimiterIndex);
                    break;
                }
                sb.Append(receivedData);
            }
            string json = sb.ToString();

            Console.WriteLine("READ JSON FROM STREAM");
            var data = JsonConvert.DeserializeObject<T>(json, settings);
            Console.WriteLine("DESERIALIZED JSON");
            //check if data type is ShipNode[,]
            if (data.GetType() == typeof(ShipNode[,])) {
                Console.WriteLine("DATA TYPE IS SHIPNODE[,]");
                ShipNode[,] board = (ShipNode[,])Convert.ChangeType(data, typeof(ShipNode[,]));
                for (int i = 0; i < board.GetLength(0); i++) {
                    for (int j = 0; j < board.GetLength(1); j++) {
                        if (board[i, j] != null)
                            Console.Write($" {board[i, j].Char} ");
                        else
                            Console.Write(" . ");
                    }
                    Console.WriteLine();
                }
            }
            return data;
        }

        static async Task<int> SendData<T>(TcpClient client, T data) {
            Console.WriteLine("SENDING");
            NetworkStream stream = client.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            string json = JsonConvert.SerializeObject(data, settings);
            await writer.WriteAsync(json);
            await writer.WriteAsync("\n\n"); // Assuming "\n\n" is the delimiter
            await writer.FlushAsync();
            return 0;
        }
    }
}

