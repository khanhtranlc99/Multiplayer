using UnityEngine;

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
     private bool isLocalPlayer = false; // Có phải người chơi local không
    
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
        
     
    }
    
    void Update()
    {
         HandleKeyboardInput();
    }
    
    // Kiểm tra xem có phải người chơi local không
 
    
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
}
    
  
    
   