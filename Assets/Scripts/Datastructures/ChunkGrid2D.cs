using System;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGridTransform2D : IGridTransform2D
{
    public readonly int chunkCellCount;
    public readonly float cellSize;
    public readonly float chunkWidth;

    public ChunkGridTransform2D(int chunkCellCount, float cellSize)
    {
        this.chunkCellCount = chunkCellCount;
        this.cellSize = cellSize;
        this.chunkWidth = cellSize * chunkCellCount;
    }

    /// @return the @p pos aligned to the grid
    public Vector2 PosAligned(Vector2 pos, float tileFactor = 1.0f)
    {
        float tw = tileFactor * cellSize;
        float x = (Mathf.FloorToInt(pos.x / tw) * tw);
        float y = (Mathf.FloorToInt(pos.y / tw) * tw);
        return new Vector2(x, y);
    }

    public Vector2Int Pos2CellPos(Vector2 pos)
    {
        return Conversions.FlooredDivision(pos, cellSize);
    }

    public Vector2Int CellPos2ChunkPos(Vector2Int cellPos)
    {
        return Conversions.FlooredIntDivision(cellPos, chunkCellCount);
    }
}

public class ChunkGrid2D<Cell> where Cell : BaseGridCell2D, new()
{
    class Chunk
    {
        public readonly Vector2Int chunkPos;
        public readonly Cell[,] cells;
        public readonly ChunkGridTransform2D transform;

        public Chunk(ChunkGridTransform2D transform, Vector2Int chunkPos)
        {
            this.transform = transform;
            this.chunkPos = chunkPos;
            cells = new Cell[transform.chunkCellCount, transform.chunkCellCount];

            // create new cell for each cell
            for (int x = 0; x < transform.chunkCellCount; x++)
            {
                for (int y = 0; y < transform.chunkCellCount; y++)
                {
                    cells[x, y] = new Cell();
                    cells[x, y].SetTransform(transform);
                    cells[x, y].SetCellPos(chunkPos * transform.chunkCellCount + new Vector2Int(x, y));
                }
            }
        }

        public Cell GetCell(Vector2Int cellPos)
        {
            Vector2Int chunkCellPos = cellPos - chunkPos * transform.chunkCellCount;
            if (chunkCellPos.x >= transform.chunkCellCount || chunkCellPos.x >= transform.chunkCellCount || chunkCellPos.x < 0 || chunkCellPos.y < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return cells[chunkCellPos.x, chunkCellPos.y];
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Chunk other = (Chunk)obj;
            return chunkPos.Equals(other.chunkPos);
        }
        public override int GetHashCode()
        {
            return Hash(this.chunkPos);
        }

        public static int Hash(Vector2Int chunkPos)
        {
            return chunkPos.x + chunkPos.y;
        }
    }

    private ChunkGridTransform2D transform;
    private Dictionary<int, List<Chunk>> chunks = new Dictionary<int, List<Chunk>>();

    public ChunkGrid2D(int chunkSize, float cellSize = 1f)
    {
        transform = new ChunkGridTransform2D(chunkSize, cellSize);
    }

    public Cell GetCell(Vector2Int cellPosition)
    {
        return getOrCreateChunk(cellPosition).GetCell(cellPosition);
    }

    /// @return all cells (inclusive) in the rectangle spanned by @p cellPositionFrom and @p cellPositionTo
    public List<Cell> GetCells(Vector2Int cellPositionFrom, Vector2Int cellPositionTo)
    {
        Vector2Int from = new Vector2Int(Mathf.Min(cellPositionFrom.x, cellPositionTo.x), Mathf.Min(cellPositionFrom.y, cellPositionTo.y));
        Vector2Int to = new Vector2Int(Mathf.Max(cellPositionFrom.x, cellPositionTo.x), Mathf.Max(cellPositionFrom.y, cellPositionTo.y));
        List<Cell> cells = new List<Cell>();
        for (int x = from.x; x <= to.x; x++)
        {
            for (int y = from.y; y <= to.y; y++)
            {
                cells.Add(GetCell(new Vector2Int(x, y)));
            }
        }
        return cells;
    }

    public List<Cell> Neighbours(Vector2Int cellPosition)
    {
        Vector2Int[] offsets = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, -1),
            new Vector2Int(-1, 1), new Vector2Int(1, -1) };
        List<Cell> cells = new List<Cell>();
        for (int i = 0; i < offsets.Length; i++)
        {
            cells[i] = GetCell(cellPosition + offsets[i]);
        }
        return cells;
    }

    public IGridTransform2D GetTransform()
    {
        return transform;
    }


    // private members and methods:
    private Queue<Chunk> cachedChunks = new Queue<Chunk>();
    private static int CachedChunksNum = 4;

    private Chunk getOrCreateChunk(Vector2Int cellPos)
    {
        Vector2Int chunkPos = transform.CellPos2ChunkPos(cellPos);
        int hash = Chunk.Hash(chunkPos);

        // check for cached chunks
        foreach (Chunk cachedChunk in this.cachedChunks)
        {
            if (cachedChunk.chunkPos.Equals(chunkPos))
            {
                return cachedChunk;
            }
        }
        //

        if (!chunks.ContainsKey(hash))
        {
            chunks[hash] = new List<Chunk>();
        }

        Chunk chunk = null;
        foreach (Chunk c in chunks[hash])
        {
            if (c.chunkPos.Equals(chunkPos))
            {
                chunk = c;
                break;
            }
        }

        if (chunk == null)
        {
            chunk = new Chunk(transform, chunkPos);
            chunks[hash].Add(chunk);
        }

        // add to cache
        cachedChunks.Enqueue(chunk);
        if (cachedChunks.Count > CachedChunksNum)
        {
            cachedChunks.Dequeue();
        }
        //

        return chunk;
    }
}

