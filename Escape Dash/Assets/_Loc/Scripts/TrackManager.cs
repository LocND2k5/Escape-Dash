using System.Collections.Generic;
using UnityEngine;

// Gắn lên Empty "TrackManager" đặt tại (0,0,0), Rotation 0.
// MAP CHẠY, PLAYER ĐỨNG YÊN: mỗi frame TrackManager kéo tất cả khúc đường lùi về phía sau
// (theo tốc độ của Player), sinh khúc mới ở phía xa và xoá khúc đã trôi qua.
//
// Pivot của mỗi Track prefab phải nằm ở ĐẦU khúc đường (mặt đường kéo dài z từ 0 -> trackLength).
// Phần tử đầu tiên của trackPrefabs phải là Track_Empty (không có chướng ngại).
public class TrackManager : MonoBehaviour
{
    [Header("Track Prefabs")]
    public GameObject[] trackPrefabs;

    [Header("Settings")]
    public float trackLength = 30f;
    public int numberOfTracks = 4;            // số khúc luôn có sẵn phía trước Player
    public float deleteBehindDistance = 20f;  // chỉ xoá khúc khi đã trôi ra sau Player khoảng này
                                              // (để Bull và camera phía sau vẫn có đường)
    public PlayerController player;           // nguồn tốc độ

    // Tổng quãng đường map đã cuộn (ScoreManager dùng để tính mét)
    public float DistanceTravelled { get; private set; }

    private readonly List<GameObject> activeTracks = new List<GameObject>();

    private void Start()
    {
        if (trackPrefabs == null || trackPrefabs.Length == 0) return;

        // Bắt đầu từ 1 khúc phía SAU Player để Bull có đường chạy.
        // 2 khúc đầu luôn trống để Player kịp làm quen.
        float z = -trackLength;
        SpawnTrack(0, z); z += trackLength;
        SpawnTrack(0, z); z += trackLength;
        while (z < numberOfTracks * trackLength)
        {
            SpawnTrack(Random.Range(0, trackPrefabs.Length), z);
            z += trackLength;
        }
    }

    private void Update()
    {
        if (trackPrefabs == null || trackPrefabs.Length == 0) return;

        // 1. Cuộn map về phía Player
        float dz = (player != null ? player.CurrentSpeed : 0f) * Time.deltaTime;
        if (dz > 0f)
        {
            DistanceTravelled += dz;
            foreach (var t in activeTracks)
                t.transform.position += Vector3.back * dz;
        }

        // 2. Xoá khúc đã trôi qua
        while (activeTracks.Count > 0 &&
               activeTracks[0].transform.position.z + trackLength < -deleteBehindDistance)
        {
            Destroy(activeTracks[0]);
            activeTracks.RemoveAt(0);
        }

        // 3. Sinh khúc mới phía xa, nối liền khúc cuối
        while (LastTrackEnd() < numberOfTracks * trackLength)
            SpawnTrack(Random.Range(0, trackPrefabs.Length), LastTrackEnd());
    }

    private float LastTrackEnd()
    {
        if (activeTracks.Count == 0) return -trackLength;
        return activeTracks[activeTracks.Count - 1].transform.position.z + trackLength;
    }

    private void SpawnTrack(int index, float z)
    {
        GameObject go = Instantiate(trackPrefabs[index],
                                    transform.position + Vector3.forward * z,
                                    Quaternion.identity,
                                    transform);
        activeTracks.Add(go);
    }
}