using System.Collections.Generic;
using UnityEngine;

public interface IObstacleMap
{
    bool IsFree(Vector2 pos);

    float DistanceToObstacle(Vector2 pos);

    float Resolution();
}

public interface IGridTransform2D
{
    public Vector2Int Pos2CellPos(Vector2 pos);

    public Vector2Int CellPos2ChunkPos(Vector2Int cellPos);
}

public abstract class BaseGridCell2D
{
    public IGridTransform2D gridTransform { get; private set; }
    public Vector2Int cellPos { get; private set; }

    public void SetTransform(IGridTransform2D transform)
    {
        this.gridTransform = transform;
    }

    public void SetCellPos(Vector2Int cellPos)
    {
        this.cellPos = cellPos;
    }
}

public interface IGrid<Cell>
{
    public Cell GetCell(Vector2Int cellPosition);

    /// @return all cells (inclusive) in the rectangle spanned by @p cellPositionFrom and @p cellPositionTo
    public List<Cell> GetCells(Vector2Int cellPositionFrom, Vector2Int cellPositionTo);

    public List<Cell> Neighbours(Vector2Int cellPosition);
    public IGridTransform2D GetTransform();
}
