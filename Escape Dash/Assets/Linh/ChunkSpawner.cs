using System.Collections.Generic;
using UnityEngine;

public class ChunkSpawner : MonoBehaviour
{
    [SerializeField] Chunk[] chunkPrefabs;
    [SerializeField] float speed = 10f;
    [SerializeField] int minChunksAhead = 2;     // chunks whose end is still ahead of the player
    [SerializeField] float playerZ = 0f;
    [SerializeField] float despawnBehind = 20f;  // recycle once a chunk's end is this far behind the player
    [SerializeField] float firstChunkZ = -10f;   // where the first chunk starts

    readonly List<Chunk> active = new List<Chunk>();
    readonly Dictionary<Chunk, Queue<Chunk>> pools = new Dictionary<Chunk, Queue<Chunk>>(); // prefab -> free instances
    readonly Dictionary<Chunk, Chunk> sourceOf = new Dictionary<Chunk, Chunk>();             // instance -> prefab
    readonly List<int> bag = new List<int>();
    int lastIndex = -1;

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
            SpawnNext();
    }

    int CountAhead()
    {
        int n = 0;
        foreach (var c in active) if (c.EndZ > playerZ) n++;
        return n;
    }

    void SpawnNext()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogError("ChunkSpawner has no chunk prefabs assigned.", this);
            enabled = false;
            return;
        }

        Chunk prefab = chunkPrefabs[DrawFromBag()];
        if (prefab.Length < 0.1f)
        {
            Debug.LogError("Chunk '" + prefab.name + "' has Length ~0. Set its Length in the Inspector.", prefab);
            return;
        }

        Chunk chunk = Get(prefab);
        float startZ = active.Count > 0 ? active[active.Count - 1].EndZ : firstChunkZ;
        chunk.transform.position = new Vector3(0f, 0f, startZ);   // root = start edge of the road
        active.Add(chunk);
    }

    // Shuffle bag: every chunk is used once before any repeats
    int DrawFromBag()
    {
        if (bag.Count == 0) RefillBag();
        int index = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        lastIndex = index;
        return index;
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

    // Pooling
    Chunk Get(Chunk prefab)
    {
        if (!pools.TryGetValue(prefab, out var q))
            pools[prefab] = q = new Queue<Chunk>();

        if (q.Count > 0)
        {
            Chunk c = q.Dequeue();
            c.gameObject.SetActive(true);
            return c;
        }

        Chunk created = Instantiate(prefab, transform);
        sourceOf[created] = prefab;
        return created;
    }

    void Release(Chunk c)
    {
        c.gameObject.SetActive(false);
        pools[sourceOf[c]].Enqueue(c);
    }
}