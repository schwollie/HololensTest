using System;
using System.Collections.Generic;
using UnityEngine;


public class MapCell : BaseGridCell2D
{
    public float height { get; set; } = 0;
    public bool clear = false;

    public MapCell() { }
}

public class GridMap : MonoBehaviour
{
    public float cellSize = 0.1f;
    public int chunkCellCount = 100;

    public float minClearanceHeight = 1.5f;

    private ChunkGrid2D<MapCell> gridMap;

    public GridMap()
    {
        gridMap = new ChunkGrid2D<MapCell>(chunkCellCount, cellSize);
    }
    public MapCell GetCell(Vector2 position)
    {
        return GetCell(gridMap.GetTransform().Pos2CellPos(position));
    }

    /// @return all cells (inclusive) in the rectangle spanned by @p positionFrom and @p positionTo
    public List<MapCell> GetCells(Vector2 positionFrom, Vector2 positionTo)
    {
        var transform = gridMap.GetTransform();
        return gridMap.GetCells(transform.Pos2CellPos(positionFrom), transform.Pos2CellPos(positionTo));
    }

}
