using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    [SerializeField] Chunk startChunkPrefab;     // optional: empty road placed first, NOT part of the random bag
    [SerializeField] Chunk[] chunkPrefabs;       // random chunks (shuffle bag)
    [SerializeField] int copiesPerChunk = 1;     // instances created per chunk at load; never more during play
    [SerializeField] float speed = 10f;
    [SerializeField] int minChunksAhead = 3;     // chunks whose end is still ahead of the player
    [SerializeField] float playerZ = 0f;
    [SerializeField] float despawnBehind = 20f;  // recycle once a chunk's end is this far behind the player
    [SerializeField] float firstChunkZ = -10f;   // where the first chunk starts

    readonly List<Chunk> active = new List<Chunk>();
    readonly Dictionary<Chunk, Queue<Chunk>> pools = new Dictionary<Chunk, Queue<Chunk>>(); // prefab -> free instances
    readonly Dictionary<Chunk, Chunk> sourceOf = new Dictionary<Chunk, Chunk>();             // instance -> prefab
    readonly List<int> bag = new List<int>();
    int lastIndex = -1;

    void Start()
    {
        // Create every instance once, up front, so nothing is instantiated during play (no lag spikes)
        if (startChunkPrefab != null) Prewarm(startChunkPrefab, 1);
        if (chunkPrefabs != null)
            foreach (var p in chunkPrefabs) Prewarm(p, Mathf.Max(1, copiesPerChunk));

        if (startChunkPrefab != null) Place(startChunkPrefab);
    }

    void Update()
    {
        // 1. Move all chunks toward the player
        Vector3 step = Vector3.back * (speed * Time.deltaTime);
        foreach (var c in active) c.transform.position += step;

        // 2. Recycle chunks fully behind the player
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i].EndZ < playerZ - despawnBehind)
            {
                Release(active[i]);
                active.RemoveAt(i);
            }
        }

        // 3. Keep enough chunks ahead (safety limit prevents freezes)
        int safety = 10;
        while (CountAhead() < minChunksAhead && safety-- > 0)
            if (!SpawnNext()) break;   // nothing free right now: try again next frame
    }

    int CountAhead()
    {
        int n = 0;
        foreach (var c in active) if (c.EndZ > playerZ) n++;
        return n;
    }

    bool SpawnNext()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogError("ChunkSpawner has no chunk prefabs assigned.", this);
            enabled = false;
            return false;
        }

        int index = DrawFreeIndex();
        return index >= 0 && Place(chunkPrefabs[index]);
    }

    // Puts a chunk at the end of the last active chunk (or at firstChunkZ if none)
    bool Place(Chunk prefab)
    {
        if (prefab.Length < 0.1f)
        {
            Debug.LogError("Chunk '" + prefab.name + "' has Length ~0. Set its Length in the Inspector.", prefab);
            return false;
        }

        Chunk chunk = Get(prefab);
        if (chunk == null) return false;

        float startZ = active.Count > 0 ? active[active.Count - 1].EndZ : firstChunkZ;
        chunk.transform.position = new Vector3(0f, 0f, startZ);   // root = start edge of the road
        active.Add(chunk);
        return true;
    }

    // ---- Shuffle bag that only hands out chunks that are free right now ----
    // A chunk that is still on the road is skipped and stays in the bag for a later draw.
    int DrawFreeIndex()
    {
        if (bag.Count == 0) RefillBag();

        for (int i = bag.Count - 1; i >= 0; i--)
        {
            int idx = bag[i];
            if (HasFree(chunkPrefabs[idx]))
            {
                bag.RemoveAt(i);
                lastIndex = idx;
                return idx;
            }
        }

        // Every chunk left in the bag is still on the road: use any free chunk instead
        int n = chunkPrefabs.Length;
        int start = Random.Range(0, n);
        for (int k = 0; k < n; k++)
        {
            int idx = (start + k) % n;
            if (idx != lastIndex && HasFree(chunkPrefabs[idx])) { lastIndex = idx; return idx; }
        }
        return -1;
    }

    void RefillBag()
    {
        for (int i = 0; i < chunkPrefabs.Length; i++) bag.Add(i);

        for (int i = bag.Count - 1; i > 0; i--)   // Fisher-Yates
        {
            int j = Random.Range(0, i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }

        // Avoid the same chunk twice in a row across bags
        int top = bag.Count - 1;
        if (bag.Count > 1 && bag[top] == lastIndex)
            (bag[top], bag[0]) = (bag[0], bag[top]);
    }

    // ---- Pooling (fixed size: instances are created only in Prewarm) ----
    void Prewarm(Chunk prefab, int count)
    {
        if (!pools.TryGetValue(prefab, out var q))
            pools[prefab] = q = new Queue<Chunk>();

        for (int i = 0; i < count; i++)
        {
            Chunk c = Instantiate(prefab, transform);
            c.gameObject.SetActive(false);
            sourceOf[c] = prefab;
            q.Enqueue(c);
        }
    }

    bool HasFree(Chunk prefab) => pools.TryGetValue(prefab, out var q) && q.Count > 0;

    Chunk Get(Chunk prefab)
    {
        if (!HasFree(prefab)) return null;
        Chunk c = pools[prefab].Dequeue();
        c.gameObject.SetActive(true);
        return c;
    }

    void Release(Chunk c)
    {
        c.gameObject.SetActive(false);
        pools[sourceOf[c]].Enqueue(c);
    }
}