using UnityEngine;

public class DrawLanes : MonoBehaviour
{
    [Header("Lane Settings")]
    public float laneDistance = 0.5f; // Khoảng cách làn (phải bằng với Lane Distance của Player)
    public float mapLength = 10f; // Chiều dài của Map (bằng với Track Length)
    public Color lineColor = Color.yellow; // Màu của vạch kẻ đường

    void Start()
    {
        // Tạo 3 vạch kẻ đường cho 3 làn
        CreateLine(0);              // Làn giữa
        CreateLine(-laneDistance);  // Làn trái
        CreateLine(laneDistance);   // Làn phải
    }

    private void CreateLine(float xOffset)
    {
        // Tạo ra một khối hộp mỏng
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        // Tắt va chạm (Collider) để nhân vật không bị vấp
        Destroy(line.GetComponent<Collider>());

        // Đặt tên cho dễ phân biệt
        line.name = "Lane_Line_" + xOffset;

        // Đặt line làm con của Map này để khi Map bị xoá, vạch cũng mất theo
        line.transform.SetParent(transform);

        // Chỉnh tọa độ thực (World Space) cho vạch kẻ:
        // Đặt nó nổi lên trên mặt đất một chút (Y = 0.01) để không bị lấp
        // X = đúng vị trí làn đường so với tâm của map
        line.transform.position = transform.position + new Vector3(xOffset, 0.01f, 0);

        // Chỉnh kích thước thực (World Space) của vạch kẻ: 
        // Bề ngang mảnh (0.05), độ mỏng (0.01), chiều dài bằng đúng mapLength
        line.transform.localScale = new Vector3(0.05f, 0.01f, mapLength);

        // Sơn màu cho vạch kẻ đường
        Renderer rend = line.GetComponent<Renderer>();
        if (rend != null)
        {
            // Tạo một material mới và tô màu vàng (hoặc màu bạn chọn)
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = lineColor;
            rend.material = mat;
        }
    }
}
