 
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

class Program
{
    static List<int> numberList = new List<int>();
    static List<TcpClient> connectedClients = new List<TcpClient>();
    static Dictionary<TcpClient, int> clientIndexMap = new Dictionary<TcpClient, int>();
    static Dictionary<int, Vector3Data> playerPositions = new Dictionary<int, Vector3Data>(); // Vị trí của từng player
    static bool gameStarted = false;
    static object locker = new object(); // tránh race condition

    // Struct để lưu vị trí
    public struct Vector3Data
    {
        public float x, y, z;
        public Vector3Data(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }
    }

    static void Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        int port = 8888;
        TcpListener server = new TcpListener(IPAddress.Any, port);
        server.Start();

        Console.WriteLine($"✅ Server đang chạy tại cổng {port}...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Thread thread = new Thread(HandleClient);
            thread.Start(client);
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;

        lock (locker)
        {
            connectedClients.Add(client);
        }

        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        try
        {
            while (true)
            {
                string command = reader.ReadLine();
                if (command == null) break;

                Console.WriteLine($"📩 Nhận từ client: {command}");

                if (command.ToUpper() == "CONNECT")
                {
                    lock (locker)
                    {
                        numberList.Add(1);
                        int playerIndex = numberList.Count - 1;
                        clientIndexMap[client] = playerIndex;
                        
                        // Khởi tạo vị trí mặc định cho player mới
                        playerPositions[playerIndex] = new Vector3Data(0, 0, 0);
                    }

                    byte[] data = ConvertListToBytes(numberList);
                    string base64 = Convert.ToBase64String(data);

                    writer.WriteLine(base64); // Gửi cho người mới
                    BroadcastAllExcept(client, base64); // Gửi cho người cũ

                    Console.WriteLine("📤 Đã gửi danh sách cho toàn bộ client");
                }
                else if (command.ToUpper() == "READY")
                {
                    lock (locker)
                    {
                        if (clientIndexMap.ContainsKey(client))
                        {
                            int index = clientIndexMap[client];
                            numberList[index] = 2; // đổi từ 1 → 2
                            
                            // Kiểm tra xem có đủ 3 người chơi sẵn sàng chưa
                            int readyCount = 0;
                            foreach (int state in numberList)
                            {
                                if (state == 2) readyCount++;
                            }
                            
                            if (readyCount >= 3 && !gameStarted)
                            {
                                gameStarted = true;
                                Console.WriteLine("🎮 Bắt đầu game với " + readyCount + " người chơi!");
                                
                                // Gửi lệnh START_GAME cho tất cả client đã sẵn sàng
                                BroadcastToReadyPlayers("START_GAME");
                            }
                        }
                    }

                    byte[] data = ConvertListToBytes(numberList);
                    string base64 = Convert.ToBase64String(data);
                    BroadcastAll(base64); // Gửi danh sách mới cho tất cả
                    Console.WriteLine("🚦 Cập nhật: client READY");
                }
                else if (command.StartsWith("POSITION:"))
                {
                    // Format: POSITION:x,y,z
                    string[] parts = command.Split(':');
                    if (parts.Length == 2)
                    {
                        string[] coords = parts[1].Split(',');
                        if (coords.Length == 3 && float.TryParse(coords[0], out float x) && 
                            float.TryParse(coords[1], out float y) && float.TryParse(coords[2], out float z))
                        {
                            lock (locker)
                            {
                                if (clientIndexMap.ContainsKey(client))
                                {
                                    int playerIndex = clientIndexMap[client];
                                    playerPositions[playerIndex] = new Vector3Data(x, y, z);
                                    
                                    // Gửi vị trí mới cho tất cả client khác
                                    string positionUpdate = $"UPDATE_POSITION:{playerIndex}:{x},{y},{z}";
                                    BroadcastAllExcept(client, positionUpdate);
                                }
                            }
                        }
                    }
                }
                else
                {
                    writer.WriteLine("❌ Lệnh không hợp lệ.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Lỗi client: " + ex.Message);
        }

        // Cleanup sau khi client rời đi
        lock (locker)
        {
            connectedClients.Remove(client);
            if (clientIndexMap.ContainsKey(client))
            {
                int idx = clientIndexMap[client];
                if (idx >= 0 && idx < numberList.Count)
                    numberList[idx] = 0; // Hoặc xóa nếu muốn
                clientIndexMap.Remove(client);
                playerPositions.Remove(idx);
            }
        }

        client.Close();
    }

    static byte[] ConvertListToBytes(List<int> list)
    {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            writer.Write(list.Count);
            foreach (int num in list)
                writer.Write(num);
            return ms.ToArray();
        }
    }

    static void BroadcastAll(string message)
    {
        lock (locker)
        {
            foreach (var cli in connectedClients)
            {
                try
                {
                    NetworkStream stream = cli.GetStream();
                    StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                    writer.WriteLine(message);
                }
                catch
                {
                    // Bỏ qua nếu bị lỗi
                }
            }
        }
    }

    static void BroadcastAllExcept(TcpClient exceptClient, string message)
    {
        lock (locker)
        {
            foreach (var cli in connectedClients)
            {
                if (cli == exceptClient) continue;
                try
                {
                    NetworkStream stream = cli.GetStream();
                    StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                    writer.WriteLine(message);
                }
                catch
                {
                    // Bỏ qua nếu bị lỗi
                }
            }
        }
    }

    static void BroadcastToReadyPlayers(string message)
    {
        lock (locker)
        {
            foreach (var kvp in clientIndexMap)
            {
                TcpClient cli = kvp.Key;
                int index = kvp.Value;
                
                if (index < numberList.Count && numberList[index] == 2) // Chỉ gửi cho người đã sẵn sàng
                {
                    try
                    {
                        NetworkStream stream = cli.GetStream();
                        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                        writer.WriteLine(message);
                    }
                    catch
                    {
                        // Bỏ qua nếu bị lỗi
                    }
                }
            }
        }
    }
}
