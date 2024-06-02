using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using UnityEngine.Assertions.Must;

/*public class AStarNode : DefaultPose, IComparable<AStarNode>
{
    public AStarNode predecessor;
    public float gCost = Mathf.Infinity; // path cost to this node
    public float hCost; // heuristic

    public AStarNode(float gCost, float hCost) : base(0, 0, 0)
    {
        this.gCost = gCost;
        this.hCost = hCost;
    }

    public AStarNode() : base(0, 0, 0)
    {
        gCost = Mathf.Infinity;
    }

    public float FCost()
    {
        return gCost + hCost;
    }

    public float CalcHCost(AStarNode other)
    {
        return 0;
        //return (other.mapPos - this.mapPos).magnitude;
    }

    public int CompareTo(AStarNode y)
    {
        if (this.FCost() > y.FCost())
        {
            return 1;
        }
        if (this.FCost() == y.FCost())
        {
            if (y.GetHashCode() > GetHashCode())
            {
                return -1;
            }
            return 1;
        }
        return -1;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public class AStar
{
    IMotionModel motionModel = new NoRotationMotionModel();
    IObstacleMap obstacleMap;
    NodeStorage<AStarNode> nodes = new NodeStorage<AStarNode>();
    double timeOut;
    DateTime startTime;

    public AStar(IObstacleMap obstacelMap, double timeOut)
    {
        this.obstacleMap = obstacelMap;
        this.timeOut = timeOut;
    }

    public bool NodesIsFree(AStarNode node)
    {
        return obstacleMap.IsFree(node.mapPos);
    }

    public void CheckTimeOut()
    {
        if ((System.DateTime.Now - startTime).TotalSeconds > timeOut)
        {
            throw new PathTimeoutException("Current explored nodes: " + nodes.GetCount(), timeOut);
        }
    }

    public List<IPose> FinalizePath(AStarNode start, AStarNode target)
    {
        List<IPose> path = new List<IPose>();

        AStarNode current = target;
        while (current != null && current != start)
        {
            CheckTimeOut();
            path.Add(current);
            current = current.predecessor;
        }
        path.Add(current);

        path.Reverse();
        return path;
    }


    public List<IPose> FindPath(IPose startPos, IPose targetPos)
    {
        startTime = System.DateTime.Now;

        var openList = new SortedSet<AStarNode>();
        var closedList = new HashSet<AStarNode>();

        var startNode = nodes.GetNode(startPos);
        var targetNode = nodes.GetNode(targetPos);

        if (!NodesIsFree(startNode) || !NodesIsFree(targetNode)) { throw new NoPathException("Start or target are not valid poses."); }

        startNode.gCost = 0;
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            CheckTimeOut();

            var minFNode = openList.Min;
            openList.Remove(minFNode);

            if (minFNode.Equals(targetPos))
            {
                return FinalizePath(startNode, targetNode); // found path
            }

            closedList.Add(minFNode);

            var neighbours = nodes.Neighbours(minFNode, motionModel);
            foreach (var neighbour in neighbours)
            {

                if (closedList.Contains(neighbour.node) || !NodesIsFree(neighbour.node))
                {
                    continue;
                }

                float newGCost = minFNode.gCost + neighbour.cost;
                if (newGCost < neighbour.node.gCost)
                {
                    openList.Remove(neighbour.node);
                    neighbour.node.predecessor = minFNode;
                    neighbour.node.gCost = newGCost;
                    neighbour.node.hCost = targetNode.CalcHCost(neighbour.node);
                    openList.Add(neighbour.node);
                }
            }
        }

        throw new NoPathException();
    }
}


public class GridAStarPlanner : IGridPlanner
{
    public static List<IPose> Path(IObstacleMap obstacleMap, IPose startPos, IPose targetPos, double timeOut = 10)
    {
        return (new AStar(obstacleMap, timeOut)).FindPath(startPos, targetPos);
    }
}*/
