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

    TcpClient client;
    NetworkStream stream;
    StreamWriter writer;
    StreamReader reader;
    Thread listenThread;
    List<int> currentList = new List<int>();

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
            client = new TcpClient("172.31.98.39", 8888); // ⚠️ sửa IP nếu cần
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
        catch (IOException)
        {
            Debug.Log("🔌 Kết nối bị đóng.");
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
