using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoPathException : Exception
{
    public NoPathException() : base("No Path was found!") { }

    public NoPathException(string msg) : base("No Path was found: '" + msg + "'.") { }
}

public class PathTimeoutException : Exception
{
    public PathTimeoutException(double timeOut) : base("Path generation timed out after " + timeOut + "!") { }

    public PathTimeoutException(string msg, double timeOut) : base("Path generation timed out after " + timeOut + ": '" + msg + "'.") { }
}

public interface IGridPlannerCell
{
    public bool IsFree();
}

public interface IGridPlanner
{
    public static List<IConfiguration> Path(IObstacleMap grid, Vector2Int startPos, Vector2Int targetPos)
    {
        throw new NotImplementedException();
    }
}
