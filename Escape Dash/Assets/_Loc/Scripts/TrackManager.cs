using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    [Header("Track Prefabs")]
    [Tooltip("Kéo thả các Prefab đường chạy (Track) vào đây")]
    public GameObject[] trackPrefabs;
    
    [Header("Settings")]
    public float zSpawn = 0;
    public float trackLength = 30f; // Chiều dài của mỗi khối track
    public int numberOfTracks = 5; // Số lượng track luôn hiển thị trên màn hình
    public Transform playerTransform;
    
    private List<GameObject> activeTracks = new List<GameObject>();

    void Start()
    {
        // Tạo ra các track ban đầu khi game bắt đầu
        for (int i = 0; i < numberOfTracks; i++)
        {
            if (i == 0)
                SpawnTrack(0); // Track đầu tiên (thường là an toàn, không có chướng ngại vật)
            else
                SpawnTrack(Random.Range(0, trackPrefabs.Length));
        }
    }

    void Update()
    {
        // Khi player chạy qua một track, tạo track mới ở phía trước và xóa track ở phía sau
        if (playerTransform != null && playerTransform.position.z - trackLength > zSpawn - (numberOfTracks * trackLength))
        {
            SpawnTrack(Random.Range(0, trackPrefabs.Length));
            DeleteTrack();
        }
    }

    public void SpawnTrack(int trackIndex)
    {
        GameObject go = Instantiate(trackPrefabs[trackIndex], transform.forward * zSpawn, transform.rotation);
        // Gom track mới vào trong GameObject TrackManager để Hierarchy gọn gàng hơn (tuỳ chọn)
        go.transform.SetParent(transform);
        activeTracks.Add(go);
        zSpawn += trackLength;
    }

    private void DeleteTrack()
    {
        Destroy(activeTracks[0]);
        activeTracks.RemoveAt(0);
    }
}
