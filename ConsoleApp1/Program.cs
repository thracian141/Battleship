using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Battleship {
    [Serializable]
    [JsonObject]
    public class ShipNode {
        [JsonProperty("Char")]
        public char Char { get; set; }
        [JsonProperty("ShipId")]
        public string ShipId { get; set; }
        [JsonProperty("HasBeenHit")]
        public bool HasBeenHit { get; set; }
        public ShipNode(char Char, string ShipId) {
            this.Char = Char;
            this.ShipId = ShipId;
            this.HasBeenHit = false;
        }
    }
    [Serializable]
    [JsonObject]
    public class Ship {
        [JsonProperty("Id")]
        public string Id { get; set; }
        [JsonProperty("Type")]
        public string Type { get; set; }
        [JsonProperty("Shape")]
        public ShipNode[,] Shape { get; set; }
        [JsonIgnore]
        public static Random rotator = new Random();
        [JsonIgnore]
        public static int IdMaker = 1;
        public Ship(string Type) {
            this.Id = IdMaker.ToString();
            IdMaker++;

            this.Type = Type;
            if (Type == "Submarine") {
                this.Shape = new ShipNode[1, 3] {
                    {new ShipNode('@', Id), new ShipNode('@', Id), new ShipNode('@', Id)}
                };
            }
            if (Type == "Carrier") {
                this.Shape = new ShipNode[2, 4] {
                    {new ShipNode('#', Id), new ShipNode('#', Id), new ShipNode('#', Id), new ShipNode('#', Id) },
                    {new ShipNode('#', Id), new ShipNode('#', Id), new ShipNode('#', Id), new ShipNode('#', Id) }
                };

            }
            if (Type == "Cruiser") {
                this.Shape = new ShipNode[2, 3] {
                    {new ShipNode('+', Id), new ShipNode('+', Id), new ShipNode('+', Id)},
                    {new ShipNode('+', Id), new ShipNode('+', Id), new ShipNode('+', Id)}
                };
            }
            if (Type == "BigFish") {
                this.Shape = new ShipNode[1, 2]
                {
                    {new ShipNode('F', Id), new ShipNode('F', Id) }
                };
            }
            if (Type == "FishingBoat") {
                this.Shape = new ShipNode[2, 2]
                {
                    {new ShipNode('$', Id), new ShipNode('$', Id)},
                    {new ShipNode('$', Id), new ShipNode('$', Id)}
                };
            }

            int randomNumber = rotator.Next(2);
            // 50% chance to rotate 90 degrees
            if (randomNumber == 0) {
                int rows = this.Shape.GetLength(0);
                int columns = this.Shape.GetLength(1);

                ShipNode[,] rotatedArray = new ShipNode[columns, rows];

                for (int i = 0; i < rows; i++) {
                    for (int j = 0; j < columns; j++) {
                        rotatedArray[j, i] = this.Shape[i, j];
                    }
                }
                this.Shape = rotatedArray;
            }
        }
    }

    public class Board {
        public static ShipNode[,] RedBoard { get; set; }
        public static ShipNode[,] BlueBoard { get; set; }
        public static List<Ship> BlueShips { get; set; }
        public static List<Ship> RedShips { get; set; }
        public static Random random = new Random();

        public Board(int BoardDimensions) {
            int NumberOfShips = BoardDimensions / 2 - 1;

            RedBoard = new ShipNode[BoardDimensions, BoardDimensions];
            BlueBoard = new ShipNode[BoardDimensions, BoardDimensions];
            BlueShips = new List<Ship>();
            RedShips = new List<Ship>();

            BlueShips.Add(new Ship("Carrier"));
            RedShips.Add(new Ship("Carrier"));

            int numberLeft = (int)((NumberOfShips - 1) * 0.5);

            //ships added to list V
            for (int i = 0; i < numberLeft; i++) {
                BlueShips.Add(new Ship("Cruiser"));
                BlueShips.Add(new Ship("Submarine"));
                BlueShips.Add(new Ship("BigFish"));
                BlueShips.Add(new Ship("FishingBoat"));

                RedShips.Add(new Ship("Cruiser"));
                RedShips.Add(new Ship("Submarine"));
                RedShips.Add(new Ship("BigFish"));
                RedShips.Add(new Ship("FishingBoat"));
            }
        }

        public static void PlaceShipsRandomly(ShipNode[,] board, Ship[] ships) {
            foreach (var ship in ships) {
                bool placed = false;
                while (!placed) {
                    int x = random.Next(0, board.GetLength(0) - ship.Shape.GetLength(0) + 1);
                    int y = random.Next(0, board.GetLength(1) - ship.Shape.GetLength(1) + 1);

                    if (CanPlaceShip(board, x, y, ship.Shape)) {
                        PlaceShip(board, x, y, ship.Shape);
                        placed = true;
                    }
                }
            }
        }

        public static bool CanPlaceShip(ShipNode[,] board, int x, int y, ShipNode[,] shape) {
            int rows = shape.GetLength(0);
            int columns = shape.GetLength(1);

            // Check for neighboring ship nodes
            for (int i = x - 1; i < x + rows + 1; i++) {
                for (int j = y - 1; j < y + columns + 1; j++) {
                    if (i >= 0 && i < board.GetLength(0) && j >= 0 && j < board.GetLength(1)) {
                        if (board[i, j] != null) {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public static void PlaceShip(ShipNode[,] board, int x, int y, ShipNode[,] shape) {
            int rows = shape.GetLength(0);
            int columns = shape.GetLength(1);

            for (int i = 0; i < rows; i++) {
                for (int j = 0; j < columns; j++) {
                    int newX = x + i;
                    int newY = y + j;
                    board[newX, newY] = shape[i, j];
                }
            }
        }

        public static async Task<int> InitializeBoards(int boardSize) {
            Board redBoard = new Board(boardSize);
            Board blueBoard = new Board(boardSize);

            Board.PlaceShipsRandomly(Board.BlueBoard, Board.BlueShips.ToArray());

            return 0;
        }
    }

    public class Program {
        static JsonSerializerSettings settings = new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        public static void PrintBoard(ShipNode[,] board) {
            int length = board.GetLength(0);
            string inset = new string(' ', length + length / 2 - 3);
            Console.WriteLine(inset + "YOUR BOARD:");
            string underscores = new string('_', length * 3);
            Console.WriteLine("   " + underscores);


            Console.Write("   ");
            for (int i = 0; i < length; i++) {
                char character = (char)('A' + i);
                Console.Write($" {character} ");
            }
            Console.WriteLine();
            string dashes = new string('-', length * 3);
            Console.WriteLine("   " + dashes);
            for (int i = 0; i < length; i++) {
                Console.ForegroundColor = ConsoleColor.White;
                string formattedNumber = i < 10 ? " " + i : i.ToString();
                Console.Write(formattedNumber + "|");
                for (int j = 0; j < length; j++) {
                    if (board[i, j] == null) {
                        Console.BackgroundColor = ConsoleColor.Cyan;
                        Console.Write("   ");
                    } else {
                        if (board[i, j].Char == '@')
                            Console.BackgroundColor = ConsoleColor.DarkGray;
                        else if (board[i, j].Char == '#')
                            Console.BackgroundColor = ConsoleColor.Red;
                        else if (board[i, j].Char == '+')
                            Console.BackgroundColor = ConsoleColor.Blue;
                        else if (board[i, j].Char == 'F')
                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                        else if (board[i, j].Char == '$')
                            Console.BackgroundColor = ConsoleColor.DarkYellow;

                        if (board[i, j].HasBeenHit) {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.Write(" X ");
                        } else {
                            Console.ForegroundColor = ConsoleColor.White;

                            Console.Write($" {board[i, j].Char} ");
                        }
                    }
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
            }
        }
        public static void PrintEnemyBoard(ShipNode[,] board) {
            int length = board.GetLength(0);
            string inset = new string(' ', length + length / 2 - 3);
            Console.WriteLine(inset + "ENEMY BOARD:");
            string underscores = new string('_', length * 3);
            Console.WriteLine("   " + underscores);

            Console.Write("   ");
            for (int i = 0; i < length; i++) {
                char character = (char)('A' + i);
                Console.Write($" {character} ");
            }
            Console.WriteLine();
            string dashes = new string('-', length * 3);
            Console.WriteLine("   " + dashes);


            for (int i = 0; i < length; i++) {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                string formattedNumber = i < 10 ? " " + i : i.ToString();
                Console.Write(formattedNumber + "|");
                for (int j = 0; j < length; j++) {
                    if (board[i, j] == null) {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($" ~ ");
                    } else {
                        if (board[i, j].HasBeenHit) {
                            if (board[i, j].Char == '@')
                                Console.BackgroundColor = ConsoleColor.DarkGray;
                            else if (board[i, j].Char == '#')
                                Console.BackgroundColor = ConsoleColor.Red;
                            else if (board[i, j].Char == '+')
                                Console.BackgroundColor = ConsoleColor.Blue;
                            else if (board[i, j].Char == 'F')
                                Console.BackgroundColor = ConsoleColor.DarkGreen;
                            else if (board[i, j].Char == '$')
                                Console.BackgroundColor = ConsoleColor.DarkYellow;

                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.Write(" X ");
                        } else {
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write(" ~ ");
                        }
                    }
                }
                Console.WriteLine();
            }
        }
        public static void Play() {
            Console.Clear();
            Console.WriteLine($"Your ships left: {Board.BlueShips.Count} | Enemy ships left: {Board.RedShips.Count}");

            PrintBoard(Board.BlueBoard);
            Console.WriteLine();
            PrintEnemyBoard(Board.RedBoard);
            Shoot();
        }
        public static void Shoot() {
            Console.Write("Shoot at: ");
            string turn = Console.ReadLine();
            string pattern = @"^[A-Z]{1," + Board.BlueBoard.GetLength(0) + @"}[1-9][0-9]*$";
            Regex regex = new Regex(pattern);

            if (regex.IsMatch(turn)) {
                int i = int.Parse((turn[0] - 'A').ToString());
                int j = int.Parse(turn[1].ToString());
                if (Board.RedBoard[j, i] == null)
                    Console.WriteLine("Miss!");
                else {
                    string shipName = Board.RedShips.Where(x => x.Id == Board.RedBoard[j, i].ShipId).First().Type;
                    Board.RedBoard[j, i].HasBeenHit = true;
                    Console.WriteLine($"You've hit a {shipName}!");
                    string shipId = Board.RedShips.Where(x => x.Id == Board.RedBoard[j, i].ShipId).First().Id;
                    bool isSunkFlag = true;
                    List<ShipNode> HitShipNodes = new List<ShipNode>();
                    for (int a = 0; a < Board.BlueBoard.GetLength(0); a++) {
                        for (int b = 0; b < Board.BlueBoard.GetLength(0); b++) {
                            if (Board.RedBoard[a, b] != null && Board.RedBoard[a, b].ShipId == shipId) {
                                HitShipNodes.Add(Board.RedBoard[a, b]);
                            }
                        }
                    }
                    foreach (var x in HitShipNodes) {
                        if (!x.HasBeenHit) {
                            isSunkFlag = false;
                        }
                    }
                    if (isSunkFlag) {
                        Board.RedShips.Remove(Board.RedShips.Where(x => x.Id == Board.RedBoard[j, i].ShipId).First());
                    }
                }
            } else {
                Shoot();
            }
        }
        static async Task<int> ReceiveIntData(NetworkStream stream) {
            Console.WriteLine("RECEIVING");
            byte[] buffer = new byte[4];
            await stream.ReadAsync(buffer, 0, buffer.Length);
            int data = BitConverter.ToInt32(buffer, 0);
            Console.WriteLine("RECEIVED INT");
            return data;
        }

        static async Task<int> SendIntData(NetworkStream stream, int data) {
            Console.WriteLine("SENDING");
            byte[] buffer = BitConverter.GetBytes(data);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            return 0;
        }
        static async Task<int> SendData<T>(NetworkStream stream, T data) {
            Console.WriteLine("ENTERED SEND DATA METHOD");
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 8192, leaveOpen: true)) {
                Console.WriteLine("USING STREAMWRITER");
                string json = JsonConvert.SerializeObject(data, settings);
                await writer.WriteAsync(json);
                await writer.WriteAsync("\n\n"); // Assuming "\n\n" is the delimiter
                await writer.FlushAsync();
                Console.WriteLine("AWAITED STREAM WRITE ASYNC");
            }
            Console.WriteLine("EXITED USING STATEMENT");
            return 0;
        }

        static async Task<T> ReceiveData<T>(NetworkStream stream) {
            using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), true, 8192, leaveOpen: true)) {
                StringBuilder builder = new StringBuilder();
                char[] buffer = new char[8192];
                int charsRead;

                while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                    string receivedData = new string(buffer, 0, charsRead);
                    int delimiterIndex = receivedData.IndexOf("\n\n"); // Assuming "\n\n" is the delimiter
                    if (delimiterIndex >= 0) {
                        builder.Append(receivedData, 0, delimiterIndex);
                        break;
                    }
                    builder.Append(receivedData);
                }

                string completeJson = builder.ToString();
                var data = JsonConvert.DeserializeObject<T>(completeJson, settings);
                return data;
            }
        }

        static void Main(string[] args) {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args) {

            int port = 12345;
            //Console.Write("Input server IP: ");
            IPAddress serverIp = Array.FindLast(Dns.GetHostEntry(string.Empty).AddressList,
               a => a.AddressFamily == AddressFamily.InterNetwork);
            //string serverIp = Console.ReadLine(); 

            TcpClient client = new TcpClient(serverIp.ToString(), port);
            NetworkStream stream = client.GetStream();

            Console.WriteLine("Connected to the server.");
            Thread.Sleep(600);
            Console.Clear();


            Console.WriteLine("Welcome to BATTLESHIP!");

            Console.Write("If you are the host, input 'true', otherwise 'false': ");
            bool isHost = bool.Parse(Console.ReadLine());
            if (isHost) {
                Console.WriteLine();
                Console.WriteLine("Input an integer board size (recommended: 10-16):");
                int boardSize = int.Parse(Console.ReadLine());
                Console.WriteLine($"You've selected a board size of {boardSize}x{boardSize}!");
                Console.WriteLine();

                Thread.Sleep(600);
                await SendIntData(stream, boardSize);
                Console.WriteLine("SENDING BOARD SIZE");

                await Board.InitializeBoards(boardSize);

                await SendData(stream, Board.BlueBoard);
                Board.RedBoard = await ReceiveData<ShipNode[,]>(stream);
            } else {
                int boardSize = await ReceiveIntData(stream);
                await Board.InitializeBoards(boardSize);

                Board.RedBoard = await ReceiveData<ShipNode[,]>(stream);
                await SendData(stream, Board.BlueBoard);

            }

            while (true) {
                if (Board.RedShips.Count <= 0) {
                    Console.Clear();
                    Console.WriteLine("You've won!");
                    break;
                } else if (Board.BlueShips.Count <= 0) {
                    Console.Clear();
                    Console.WriteLine("You've lost!");
                    break;
                }
                Play();
            }

            stream.Close();
            client.Close();
        }
    }
}
