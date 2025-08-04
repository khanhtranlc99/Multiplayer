using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ClientController : MonoBehaviour
{
    public List<Image> lsAvatarClients;
    public List<Material> lsMaterials;
    public List<Transform> lsTransforms;
    public Button readyButton;
    public GameObject cubePrefab; // Prefab cube để spawn

    TcpClient client;
    NetworkStream stream;
    StreamWriter writer;
    StreamReader reader;
    Thread listenThread;
    List<int> currentList = new List<int>();
    
    // Quản lý cube của các player
    Dictionary<int, GameObject> playerCubes = new Dictionary<int, GameObject>();
    int myPlayerIndex = -1;
    bool gameStarted = false;

    public CubeController cubeController;

    public Button leftBtn;
    public Button rightBtn;
    public Button upBtn;
    public Button downBtn;
 

    void Start()
    {
        // Gắn sự kiện nút Sẵn sàng
        readyButton.onClick.AddListener(OnReadyClicked);
    }

    public void Init()
    {
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient("192.168.1.9", 8888); // ⚠️ sửa IP nếu cần
            stream = client.GetStream();
            writer = new StreamWriter(stream) { AutoFlush = true };
            reader = new StreamReader(stream);

            writer.WriteLine("CONNECT");

            string base64 = reader.ReadLine();
            Debug.Log("📥 Danh sách ban đầu (base64): " + base64);

            byte[] data = Convert.FromBase64String(base64);
            currentList = ConvertBytesToList(data);

            UpdateUI(currentList);

            listenThread = new Thread(ListenToServer);
            listenThread.IsBackground = true;
            listenThread.Start();
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Lỗi kết nối server: " + ex.Message);
        }
    }

    void ListenToServer()
    {
        try
        {
            while (true)
            {
                string line = reader.ReadLine();
                if (line != null)
                {
                    Debug.Log("🔔 Server push: " + line);

                    if (line.StartsWith("START_GAME:"))
                    {
                        // Format: START_GAME:playerIndex
                        string[] parts = line.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int playerIndex))
                        {
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                StartGame(playerIndex);
                            });
                        }
                    }
                    else if (line.StartsWith("UPDATE_POSITION:"))
                    {
                        // Format: UPDATE_POSITION:playerIndex:x,y,z
                        Debug.Log($"📥 Nhận UPDATE_POSITION: {line}");
                        string[] parts = line.Split(':');
                        if (parts.Length == 3)
                        {
                            if (int.TryParse(parts[1], out int playerIndex))
                            {
                                string[] coords = parts[2].Split(',');
                                if (coords.Length == 3 && float.TryParse(coords[0], out float x) && 
                                    float.TryParse(coords[1], out float y) && float.TryParse(coords[2], out float z))
                                {
                                    Debug.Log($"📍 Parse thành công: Player {playerIndex} -> ({x},{y},{z})");
                                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                                    {
                                        UpdatePlayerPosition(playerIndex, new Vector3(x, y, z));
                                    });
                                }
                                else
                                {
                                    Debug.LogError($"❌ Không thể parse coordinates: {parts[2]}");
                                }
                            }
                            else
                            {
                                Debug.LogError($"❌ Không thể parse player index: {parts[1]}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"❌ Format UPDATE_POSITION không đúng: {line}");
                        }
                    }
                    else
                    {
                        try
                        {
                            byte[] data = Convert.FromBase64String(line);
                            List<int> list = ConvertBytesToList(data);
                            currentList = list;
                            foreach(var item in list)
                            {
                                Debug.Log(item);
                            }
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                UpdateUI(list);
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"⚠️ Không thể parse base64 list: {line}");
                            Debug.LogError($"📛 Exception: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (IOException)
        {
            Debug.Log("🔌 Kết nối bị đóng.");
        }
    }

    void StartGame(int playerIndex)
    {
        if (gameStarted) return;
        
        gameStarted = true;
        myPlayerIndex = playerIndex;
        Debug.Log($"🎮 Bắt đầu game! Player index của tôi: {myPlayerIndex}");
        Debug.Log($"📊 Danh sách player: {string.Join(",", currentList)}");
        
        // Tạo cube cho tất cả người chơi đã sẵn sàng
        int cubeCount = 0;
        for (int i = 0; i < currentList.Count; i++)
        {
            if (currentList[i] == 2 && i < lsTransforms.Count)
            {
                Debug.Log($"🎲 Tạo cube cho player {i} (trạng thái: {currentList[i]})");
                CreatePlayerCube(i);
                cubeCount++;
            }
            else
            {
                Debug.Log($"⏭️ Bỏ qua player {i} (trạng thái: {currentList[i]}, có transform: {i < lsTransforms.Count})");
            }
        }
        
        Debug.Log($"✅ Đã tạo {cubeCount} cube cho {currentList.Count} player");
        
        // Ẩn UI ready
        readyButton.gameObject.SetActive(false);
    }

    void CreatePlayerCube(int playerIndex)
    {
        Debug.Log($"🔨 Bắt đầu tạo cube cho player {playerIndex}");
        Debug.Log($"🔨 lsTransforms.Count: {lsTransforms.Count}, lsMaterials.Count: {lsMaterials.Count}");
        
        if (playerIndex >= lsTransforms.Count || playerIndex >= lsMaterials.Count) 
        {
            Debug.LogError($"❌ Không thể tạo cube cho player {playerIndex}: Index vượt quá giới hạn!");
            return;
        }
        
        // Tạo cube tại vị trí tương ứng
        Vector3 spawnPosition = lsTransforms[playerIndex].position;
        GameObject cube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);
        
        if (cube == null)
        {
            Debug.LogError($"❌ Không thể instantiate cube prefab!");
            return;
        }
        
        // Gán material tương ứng
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = lsMaterials[playerIndex];
            Debug.Log($"🎨 Gán material {playerIndex} cho cube");
        }
        else
        {
            Debug.LogWarning($"⚠️ Cube không có Renderer component!");
        }
        
        // Nếu là cube của mình, cho phép điều khiển
        if (playerIndex == myPlayerIndex)
        {
            CubeController controller = cube.GetComponent<CubeController>();
            if (controller != null)
            {
                controller.isLocalPlayer = true;
                controller.OnPositionChanged += SendPositionToServer;
                Debug.Log($"🎮 Cube {playerIndex} là của tôi - cho phép điều khiển");
            }
        }
        else
        {
            // Nếu là cube của người khác, vô hiệu hóa input
            CubeController controller = cube.GetComponent<CubeController>();
            if (controller != null)
            {
                controller.isLocalPlayer = false;
                Debug.Log($"👤 Cube {playerIndex} là của người khác - vô hiệu hóa input");
            }
        }
        
        playerCubes[playerIndex] = cube;
        Debug.Log($"✅ Tạo thành công cube cho player {playerIndex} tại vị trí {spawnPosition}");
    }

    void UpdatePlayerPosition(int playerIndex, Vector3 newPosition)
    {
        Debug.Log($"🎯 UpdatePlayerPosition được gọi: Player {playerIndex}, Vị trí {newPosition}, myPlayerIndex: {myPlayerIndex}");
        Debug.Log($"🎯 playerCubes.ContainsKey({playerIndex}): {playerCubes.ContainsKey(playerIndex)}");
        
        if (playerCubes.ContainsKey(playerIndex) && playerIndex != myPlayerIndex)
        {
            playerCubes[playerIndex].transform.position = newPosition;
            Debug.Log($"📍 Cập nhật vị trí player {playerIndex}: {newPosition}");
        }
        else
        {
            if (!playerCubes.ContainsKey(playerIndex))
            {
                Debug.LogWarning($"⚠️ Không tìm thấy cube cho player {playerIndex}");
            }
            if (playerIndex == myPlayerIndex)
            {
                Debug.Log($"ℹ️ Bỏ qua cập nhật vị trí của chính mình (player {playerIndex})");
            }
        }
    }

    void SendPositionToServer(Vector3 position)
    {
        if (writer != null && gameStarted)
        {
            string positionCommand = $"POSITION:{position.x},{position.y},{position.z}";
            writer.WriteLine(positionCommand);
            Debug.Log($"📤 Gửi vị trí lên server: {positionCommand}");
        }
        else
        {
            if (writer == null)
                Debug.LogWarning("⚠️ Writer null, không thể gửi vị trí");
            if (!gameStarted)
                Debug.LogWarning("⚠️ Game chưa bắt đầu, không thể gửi vị trí");
        }
    }

    void UpdateUI(List<int> list)
    {
        for (int i = 0; i < lsAvatarClients.Count; i++)
        {
            if (i < list.Count)
            {
                int state = list[i];
                if (state == 1)
                    lsAvatarClients[i].color = Color.red;
                else if (state == 2)
                    lsAvatarClients[i].color = Color.green;
                else
                    lsAvatarClients[i].color = Color.gray;
            }
            else
            {
                lsAvatarClients[i].color = Color.white;
            }
        }
    }

    void OnReadyClicked()
    {
        if (writer != null)
        {
            writer.WriteLine("READY");
            Debug.Log("📤 Gửi lệnh READY lên server.");
        }
    }

    List<int> ConvertBytesToList(byte[] data)
    {
        List<int> list = new List<int>();
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadInt32());
            }
        }
        return list;
    }
    
    void OnApplicationQuit()
    {
        try
        {
            listenThread?.Abort();
            writer?.Close();
            reader?.Close();
            stream?.Close();
            client?.Close();
        }
        catch { }
    }
}
