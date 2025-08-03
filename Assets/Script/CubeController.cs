using UnityEngine;
using System;

public class CubeController : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;
    
    [Header("Cài đặt gửi vị trí")]
    public float sendPositionInterval = 0.1f; // Gửi vị trí mỗi 0.1 giây
    private float lastSendTime = 0f;
    
    private Rigidbody rb;
    private Vector3 lastPosition;
    
    [Header("Multiplayer Settings")]
    public bool isLocalPlayer = false; // Có phải người chơi local không
    
    // Event để thông báo khi vị trí thay đổi
    public event Action<Vector3> OnPositionChanged;
    
    void Start()
    {
        // Lấy component Rigidbody, nếu không có thì thêm vào
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Cài đặt Rigidbody để di chuyển mượt mà hơn
        rb.freezeRotation = true; // Không cho cube xoay khi va chạm
        
        lastPosition = transform.position;
    }
    
    void Update()
    {
        // Chỉ xử lý input nếu là người chơi local
        if (isLocalPlayer)
        {
            HandleKeyboardInput();
        }
        
        // Kiểm tra và gửi vị trí nếu có thay đổi
        CheckAndSendPosition();
    }
    
    // Xử lý input từ bàn phím
    private void HandleKeyboardInput()
    {
        if (Input.GetKey(KeyCode.W))
            MoveForward();
        if (Input.GetKey(KeyCode.S))
            MoveBackward();
        if (Input.GetKey(KeyCode.A))
            MoveLeft();
        if (Input.GetKey(KeyCode.D))
            MoveRight();
    }
    
    // Kiểm tra và gửi vị trí nếu có thay đổi
    private void CheckAndSendPosition()
    {
        if (isLocalPlayer && Time.time - lastSendTime >= sendPositionInterval)
        {
            Vector3 currentPosition = transform.position;
            if (Vector3.Distance(currentPosition, lastPosition) > 0.01f) // Chỉ gửi nếu di chuyển đủ xa
            {
                OnPositionChanged?.Invoke(currentPosition);
                lastPosition = currentPosition;
                lastSendTime = Time.time;
            }
        }
    }
    
    // Di chuyển về phía trước (tiến)
    public void MoveForward()
    {
        Vector3 movement = Vector3.forward;
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
        RotateTowardsDirection(movement);
    }
    
    // Di chuyển về phía sau (lùi)
    public void MoveBackward()
    {
        Vector3 movement = Vector3.back;
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
        RotateTowardsDirection(movement);
    }
    
    // Di chuyển sang trái
    public void MoveLeft()
    {
        Vector3 movement = Vector3.left;
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
        RotateTowardsDirection(movement);
    }
    
    // Di chuyển sang phải
    public void MoveRight()
    {
        Vector3 movement = Vector3.right;
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
        RotateTowardsDirection(movement);
    }
    
    // Xoay cube theo hướng di chuyển
    private void RotateTowardsDirection(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    // Phương thức để thay đổi tốc độ di chuyển từ bên ngoài
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    // Phương thức để lấy vị trí hiện tại
    public Vector3 GetCurrentPosition()
    {
        return transform.position;
    }
    
    // Phương thức để set vị trí từ server (cho người chơi khác)
    public void SetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
}
    
  
    
   