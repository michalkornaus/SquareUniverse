using System.Collections.Generic;
using UnityEngine;
public class World
{
    public Dictionary<ChunkId, Chunk> Chunks = new Dictionary<ChunkId, Chunk>();
    public Dictionary<HeightmapId, Heightmap> Heightmaps = new Dictionary<HeightmapId, Heightmap>();
    public byte this[int x, int y, int z]
    {
        get
        {
            if (Chunks.TryGetValue(ChunkId.FromWorldPos(x, z), out Chunk _chunk))
            {
                return _chunk[x & 0xF, y, z & 0xF];
            }
            else
                return 3;
        }
        set
        {
            var chunk = Chunks[ChunkId.FromWorldPos(x, z)];
            var heightmap = Heightmaps[HeightmapId.FromWorldPos(x, z)];
            heightmap[x & 0x7F, y, z & 0x7F] = value;
            chunk[x & 0xF, y, z & 0xF] = value;
            heightmap.SaveData();
        }
    }
    public byte GetBiom(int x, int z)
    {
        var heightmap = Heightmaps[HeightmapId.FromWorldPos(x, z)];
        return heightmap[x & 0x7F, z & 0x7F];
    }
    public void SetChunkDirty(int x, int z, bool newChunk)
    {
        var chunk = Chunks[ChunkId.FromWorldPos(x, z)];
        chunk.isDirty = true;
        if (newChunk)
        {
            if (Chunks.TryGetValue(ChunkId.FromWorldPos(x - 1, z + 1), out Chunk _chunk))
            {
                _chunk.isDirty = true;
            }
            if (Chunks.TryGetValue(ChunkId.FromWorldPos(x + 1, z - 1), out _chunk))
            {
                _chunk.isDirty = true;
            }
            if (Chunks.TryGetValue(ChunkId.FromWorldPos(x + 17, z + 1), out _chunk))
            {
                _chunk.isDirty = true;
            }
            if (Chunks.TryGetValue(ChunkId.FromWorldPos(x + 1, z + 17), out _chunk))
            {
                _chunk.isDirty = true;
            }
        }
        else
        {
            var _x = x & 0xF;
            var _z = z & 0xF;
            if (_x == 0 || _x == 15)
            {
                if (Chunks.TryGetValue(ChunkId.FromWorldPos(x - (_x == 0 ? 1 : -1), z), out Chunk _chunk))
                {
                    _chunk.isDirty = true;
                }
            }
            if (_z == 0 || _z == 15)
            {
                if (Chunks.TryGetValue(ChunkId.FromWorldPos(x, z - (_z == 0 ? 1 : -1)), out Chunk _chunk))
                {
                    _chunk.isDirty = true;
                }
            }
        }
    }
}
