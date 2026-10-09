using UnityEngine;

// Rule: the chunk's root sits at the START edge of the road, the road runs forward (+Z) for `length`,
// and it is centered on X = 0. Type the real road length here; nothing is measured at runtime.
public class Chunk : MonoBehaviour
{
    [SerializeField] float length = 20f;

    public float Length => length;
    public float EndZ => transform.position.z + length;

    // Yellow lines show where the code thinks the chunk starts and ends.
    // If they don't sit on the road's two ends, fix the prefab (not the code).
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 a = transform.TransformPoint(0f, 0.1f, 0f);
        Vector3 b = transform.TransformPoint(0f, 0.1f, length);
        Gizmos.DrawLine(a + Vector3.left * 3f, a + Vector3.right * 3f);
        Gizmos.DrawLine(b + Vector3.left * 3f, b + Vector3.right * 3f);
    }
}