using UnityEngine;
using System.Collections; // Cần thiết để dùng Coroutine cho thời gian trượt

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 10f; // Tốc độ chạy thẳng
    public float laneDistance = 2.5f; // Khoảng cách giữa các làn (Lane)
    public float laneChangeSpeed = 10f; // Tốc độ chuyển làn

    [Header("Jump & Slide Settings")]
    public float jumpForce = 8f; // Lực nhảy lên
    public float gravity = -20f; // Trọng lực (Lực kéo xuống)
    public float slideDuration = 0.8f; // Thời gian duy trì trạng thái trượt

    // 0 = Làn trái, 1 = Làn giữa, 2 = Làn phải
    private int currentLane = 1; 
    private Vector3 targetPosition;
    
    // Các biến cho vật lý nhảy và chạm đất
    private float verticalVelocity;
    private bool isGrounded = true;
    private bool isSliding = false;
    private float groundY; // Lưu độ cao mặt đất ban đầu

    // Lưu lại kích thước ban đầu để phục hồi sau khi trượt
    private Vector3 originalScale;

    void Start()
    {
        targetPosition = transform.position;
        originalScale = transform.localScale;
        groundY = transform.position.y; // Lấy Y hiện tại làm mặt đất
    }

    void Update()
    {
        // 1. Nhận Input đổi làn đường (A / D / Left / Right)
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            MoveLane(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            MoveLane(1);
        }

        // 2. Nhận Input Nhảy (W / Space / Up)
        if (isGrounded && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)))
        {
            Jump();
        }

        // 3. Nhận Input Trượt (S / Down)
        if (!isSliding && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
        {
            StartCoroutine(Slide());
        }

        // 4. Tính toán trọng lực (rơi xuống)
        if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 5. Cập nhật vị trí X theo làn đường
        targetPosition = transform.position;
        if (currentLane == 0)
            targetPosition.x = -laneDistance; // Làn trái
        else if (currentLane == 1)
            targetPosition.x = 0;             // Làn giữa
        else if (currentLane == 2)
            targetPosition.x = laneDistance;  // Làn phải

        // Tính toán độ cao (Y) dựa trên vận tốc nhảy/rơi
        targetPosition.y += verticalVelocity * Time.deltaTime;

        // Giới hạn mặt đất (chạm đất thì dừng rơi)
        if (targetPosition.y <= groundY)
        {
            targetPosition.y = groundY;
            verticalVelocity = 0f;
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        // 6. Di chuyển tổng hợp: Tiến tới (Z) + Đổi làn (X mượt) + Nhảy (Y)
        
        // A) Tịnh tiến tiến tới liên tục (Trục Z)
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);
        
        // B) Cập nhật vị trí chuyển làn (Trục X) và vị trí cao độ (Trục Y)
        Vector3 newPos = new Vector3(
            Mathf.Lerp(transform.position.x, targetPosition.x, laneChangeSpeed * Time.deltaTime), // Đổi làn mượt mà
            targetPosition.y, // Nhảy lên/Rơi xuống áp dụng trực tiếp công thức vật lý
            transform.position.z // Z đã được tịnh tiến bằng Translate ở trên nên giữ nguyên
        );
        transform.position = newPos;
    }

    private void MoveLane(int direction)
    {
        currentLane += direction;
        // Giới hạn làn đường từ 0 đến 2
        currentLane = Mathf.Clamp(currentLane, 0, 2);
    }

    private void Jump()
    {
        verticalVelocity = jumpForce;
        isGrounded = false;
        
        // Hủy trạng thái trượt nếu đang trượt mà bấm nhảy
        if (isSliding)
        {
            StopAllCoroutines();
            transform.localScale = originalScale;
            isSliding = false;
        }
    }

    private IEnumerator Slide()
    {
        isSliding = true;
        
        // 1. Ép nhân vật rơi nhanh xuống đất nếu đang ở trên không
        if (!isGrounded)
        {
            verticalVelocity = -jumpForce; // Lao nhanh xuống
        }

        // 2. Thay đổi scale Y (chiều cao) giảm xuống để giả lập động tác lộn/trượt
        Vector3 slideScale = originalScale;
        slideScale.y = originalScale.y / 2f; 
        transform.localScale = slideScale;

        // 3. Chờ hết thời gian trượt
        yield return new WaitForSeconds(slideDuration);

        // 4. Phục hồi lại chiều cao ban đầu
        transform.localScale = originalScale;
        isSliding = false;
    }
}
