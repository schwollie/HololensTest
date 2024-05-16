using System;
using System.Collections.Generic;
using UnityEngine;


public class GridInfo
{
    public int chunkTileCount;
    public float tileWidth;
    public float chunkSize;

    public GridInfo(int chunkTileCount, float tileWidth)
    {
        this.chunkTileCount = chunkTileCount;
        this.tileWidth = tileWidth;
        this.chunkSize = tileWidth * chunkTileCount;
    }

}

public class Tile
{
    public float height { get; set; } = 0;
    public bool clear = false;
    public Vector2 worldPos;

    public Tile(Vector2 worldPos) {  this.worldPos = worldPos; }
}

public class Chunk
{
    
    GridInfo gridInfo;

    public Vector2Int chunkPos;
    public Vector2 worldPos;
    public Tile[,] tiles;

    public Chunk(GridInfo info, Vector2Int pos, Vector2 worldPos)
    {
        this.gridInfo = info;
        chunkPos = pos;
        this.worldPos = worldPos;
        this.tiles = new Tile[info.chunkTileCount, info.chunkTileCount];

        for (int y = 0; y < info.chunkTileCount; y++)
        {
            for (int x = 0; x < info.chunkTileCount; x++)
            {
                var tileWorldPos = new Vector2(x * info.tileWidth + worldPos.x, y * info.tileWidth + worldPos.y);
                tiles[x,y] = new Tile(tileWorldPos);
            }
        }
    }

    public Tile getTile(Vector2Int tilePos)
    {
        // TODO bug fix
        if (tilePos.x == gridInfo.chunkTileCount)
        {
            tilePos.x -= 1;
        }
        if (tilePos.y == gridInfo.chunkTileCount)
        {
            tilePos.y -= 1;
        }
        if (tilePos.x >= gridInfo.chunkTileCount || tilePos.y >= gridInfo.chunkTileCount)
        {
            throw new ArgumentOutOfRangeException("Tile pos " + tilePos + " not in range of tileCount: " + gridInfo.chunkTileCount);
        }

        return tiles[tilePos.x, tilePos.y];
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        Chunk other = (Chunk)obj;
        return chunkPos == other.chunkPos;
    }

    public override int GetHashCode()
    {
        return HashPos(chunkPos);
    }

    public static int HashPos(Vector2Int chunkPos)
    {
        return chunkPos.x + chunkPos.y;
    }
}

public class Grid
{
    Dictionary<int, List<Chunk>> chunks = new Dictionary<int, List<Chunk>>(); ///< own hash set implementation

    GridInfo gridInfo;

    public Grid(GridInfo gridInfo)
    {
        this.gridInfo = gridInfo;
    }

    // @return the chunks grid position
    public Vector2Int World2ChunkPos(Vector2 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x/gridInfo.chunkSize), Mathf.FloorToInt(worldPos.y/ gridInfo.chunkSize));
    }

    // @return the world position of a chunk
    public Vector2 ChunkPos2World(Vector2Int chunkPos)
    {
        return new Vector2(chunkPos.x * gridInfo.chunkSize, chunkPos.y * gridInfo.chunkSize);
    }

    // @return a tiles position in the chunk
    public Vector2Int World2ChunkTilePos(Vector2 worldPos)
    {
        Vector2 chunkPos = ((Vector2)World2ChunkPos(worldPos))*gridInfo.chunkSize;
        Vector2 relativePos = (worldPos - chunkPos) / gridInfo.chunkSize;
        return new Vector2Int(Mathf.FloorToInt(relativePos.x * gridInfo.chunkTileCount), Mathf.FloorToInt(relativePos.y * gridInfo.chunkTileCount));
    }


    public Chunk getOrCreateChunk(Vector2 worldPos) {
        Vector2Int chunkPos = World2ChunkPos(worldPos);
        int hash = Chunk.HashPos(chunkPos);
        if (!chunks.ContainsKey(hash))
        {
            chunks[hash] = new List<Chunk>();
        }

        Chunk chunk = null;
        foreach (Chunk c in chunks[hash])
        {
            if (c.chunkPos == chunkPos)
            {
                chunk = c;
                break;
            }
        }

        if (chunk == null)
        {
            chunk = new Chunk(gridInfo, chunkPos, ChunkPos2World(chunkPos));
            chunks[hash].Add(chunk);
        }

        return chunk;
    }

    public Tile getTile(Vector2 worldPos)
    {
        var tilePos = World2ChunkTilePos(worldPos);
        return getOrCreateChunk(worldPos).getTile(tilePos);
    }
}

public class GridMap : MonoBehaviour
{
    public float tileWidth = 0.1f;
    public int chunkTileCount = 100;

    public float minClearanceHeight = 1.5f;

    private Grid grid;

    public GridMap()
    {
        grid = new Grid(new GridInfo(chunkTileCount, tileWidth));
    }

    public Tile GetTile(Vector2 worldPos)
    {
        return grid.getTile(worldPos);
    }

    public List<Tile> GetTiles(Vector2 worldPosFrom, Vector2 worldPosTo)
    {
        List<Tile> tiles = new List<Tile>();
        var tileFrom = PosAligned(worldPosFrom);
        var tileTo = PosAligned(worldPosTo);

        var tileMin = new Vector2(Math.Min(tileFrom.x, tileTo.x), Math.Min(tileFrom.y, tileTo.y));
        var tileMax = new Vector2(Math.Max(tileFrom.x, tileTo.x), Math.Max(tileFrom.y, tileTo.y));

        for (float x = tileMin.x; x <= tileMax.x; x+=tileWidth)
        {
            for (float y = tileMin.y; y <= tileMax.y; y+=tileWidth)
            {
                tiles.Add(grid.getTile(new Vector2(x,y)));
            }
        }
        return tiles;
    }

    /// @return the @p pos aligned to the grid
    public Vector2 PosAligned(Vector2 pos, float tileFactor=1.0f)
    {
        float tw = tileFactor * tileWidth;
        float x = (Mathf.FloorToInt(pos.x / tw) * tw);
        float y = (Mathf.FloorToInt(pos.y / tw) * tw);
        return new Vector2(x, y);
    }
}
