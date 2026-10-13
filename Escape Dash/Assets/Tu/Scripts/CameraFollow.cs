using UnityEngine;

// Gắn lên Main Camera. Camera chạy theo Player trên trục Z, bám nhẹ theo trục X.
// Gợi ý: Rotation X của camera khoảng 15-20 độ để nhìn chếch xuống.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -7f);
    [SerializeField, Range(0f, 1f)] private float xFollow = 0.5f; // 0 = đứng yên theo X, 1 = bám hoàn toàn
    [SerializeField] private float xSmooth = 8f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        pos.z = target.position.z + offset.z;
        pos.y = offset.y;
        float desiredX = target.position.x * xFollow + offset.x;
        pos.x = Mathf.Lerp(pos.x, desiredX, xSmooth * Time.deltaTime);
        transform.position = pos;
    }
}
