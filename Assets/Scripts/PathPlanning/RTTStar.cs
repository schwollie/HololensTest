using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;


public class RTTStar
{
    IMotionModel motionModel = new NoRotationMotionModel();
    IObstacleMap obstacleMap;

    public RTTStar(IObstacleMap obstacelMap)
    {
        this.obstacleMap = obstacelMap;
    }

    public bool IsFree(IPose pose)
    {
        return obstacleMap.IsFree(pose.GetPos());
    }

    private float CalcCost(IPose a, IPose b)
    {
        float rotCost = 0;// Mathf.Abs(Mathf.DeltaAngle(Mathf.Rad2Deg * a.GetRotation(), Mathf.Rad2Deg * b.GetRotation())) / 180;
        float distCost = (a.GetPos() - b.GetPos()).sqrMagnitude;
        return rotCost + distCost;
    }

    private bool IsReachable(IPose from, IPose to)
    {
        int numSteps = (int)((from.GetPos() - to.GetPos()).magnitude / (obstacleMap.Resolution() * 1.4));
        for (int i = 0; i <= numSteps; i++)
        {
            float progress = (float)(i) / ((float)(numSteps));
            if (!obstacleMap.IsFree(Vector2.Lerp(from.GetPos(), to.GetPos(), progress)))
            {
                return false;
            }
        }

        return true;
    }



    /// @return a valid random pose from a given pose. If no pose was found in @p numTrys then null is returned
    private IPose NewValidPose(IPose pose, int numTrys = 10)
    {
        for (int i = 0; i < numTrys; i++)
        {
            float xOffset = RandomHelper.GenerateRandomFloatBothSigns(0.1f, 0.8f);
            float yOffset = RandomHelper.GenerateRandomFloatBothSigns(0.1f, 0.8f);
            float rotOffset = RandomHelper.GenerateRandomFloatBothSigns(0, 1);

            IPose newPose = new DefaultPose(pose.GetPos().x + xOffset, pose.GetPos().y + yOffset, pose.GetRotation() + rotOffset);
            if (IsFree(newPose))
            {
                return newPose;
            }
        }
        return null;
    }

    /// @return a new valid random pose in a given radius around start or target. If no pose was found in @p numTrys then null is returned
    private IPose NewRandomPoseCircle(IPose start, IPose target, int numTrys = 10)
    {
        float rMax = Mathf.Max((target.GetPos() - start.GetPos()).magnitude * RandomHelper.GenerateRandomFloat(1, 5), 10);
        Vector2 mid = (target.GetPos() + start.GetPos()) / 2f;
        for (int i = 0; i < numTrys; i++)
        {
            Vector2 randPos = RandomHelper.RandomPointOnCircle(rMax);
            float rot = RandomHelper.GenerateRandomFloatBothSigns(0, (float)(2 * Math.PI));
            IPose newPose = new DefaultPose(mid.x + randPos.x, mid.y + randPos.y, rot);
            if (IsFree(newPose))
            {
                return newPose;
            }
        }
        return null;
    }

    private List<IPose> GenerateRandomPosesFromExistingNodes(SimpleTree<IPose> graph, int maxNewNodesPerIt)
    {
        List<IPose> poses = new List<IPose>();
        var toBeExtendedNodes = RandomHelper.ReservoirSample(graph.nodes, maxNewNodesPerIt);
        foreach (var node in toBeExtendedNodes)
        {
            IPose newPose = NewValidPose(node.value);
            if (newPose == null) { continue; }
            poses.Add(newPose);
        }
        return poses;
    }

    private List<IPose> GenerateRandomPosesWholeMap(IPose startPose, IPose targetPose, int maxNewNodesPerIt)
    {
        List<IPose> poses = new List<IPose>();

        for (int i = 0; i < maxNewNodesPerIt; i++)
        {
            IPose newPose = NewRandomPoseCircle(startPose, targetPose);
            if (newPose == null) { continue; }
            poses.Add(newPose);
        }
        return poses;
    }

    public List<IPose> FinalizePath(List<Node<IPose>> targetToStart)
    {
        List<IPose> path = new List<IPose>();
        targetToStart.Reverse();
        foreach (var node in targetToStart)
        {
            path.Add(node.value);
        }
        return path;

    }

    /// @return ascending neighbour nodes given cost @p radius around @p node. Always contains the closest neighbour (if reachable) even if not in cost radius
    private List<Node<IPose>> GetReachableNeighbours(SimpleTree<IPose> graph, Node<IPose> node, double radius = 3)
    {
        List<Node<IPose>> neighboursAscendingDistance = new List<Node<IPose>>();
        graph.Neighbours(node, this.CalcCost, radius);
        if (neighboursAscendingDistance.Count == 0) { neighboursAscendingDistance.Add(graph.GetClosest(node, this.CalcCost)); }
        neighboursAscendingDistance = neighboursAscendingDistance.Where(n => IsReachable(n.value, node.value)).ToList();
        return neighboursAscendingDistance;
    }

    public List<IPose> FindPath(IPose startPose, IPose targetPose, int maxIterations = 50, int maxNewNodesPerIt = 50)
    {
        SimpleTree<IPose> graph = new SimpleTree<IPose>();

        var startNode = graph.Add(startPose);
        startNode.UpdateCost(0);

        for (int i = 0; i < maxIterations; i++)
        {
            // in each iteration sample new poses from config space
            List<IPose> newPoses;// = GenerateRandomPosesFromExistingNodes(graph, maxNewNodesPerIt);
            if (i % 2 == 0) { newPoses = GenerateRandomPosesWholeMap(startPose, targetPose, maxNewNodesPerIt); }
            else { newPoses = GenerateRandomPosesFromExistingNodes(graph, maxNewNodesPerIt); }

            foreach (var newPose in newPoses)
            {
                if (newPose == null) { continue; }
                var newNode = graph.Add(newPose);
                var neighboursAscendingDistance = GetReachableNeighbours(graph, newNode);
                if (neighboursAscendingDistance.Count == 0) { graph.Remove(newNode); continue; } // no neighbours can reach new node so continue

                graph.AddEdge(neighboursAscendingDistance[0], newNode, CalcCost(neighboursAscendingDistance[0].value, newPose));
                neighboursAscendingDistance.RemoveAt(0);
                foreach (var neighbour in neighboursAscendingDistance)
                {
                    float cost = CalcCost(newNode.value, neighbour.value);
                    if (newNode.GetCost() + cost < neighbour.GetCost())
                    {
                        // will override predecessor and thereby remain tree structure
                        graph.AddEdgeOverride(newNode, neighbour, cost);
                    }
                }
            }
        }

        // finaly add target node and check if path was found
        var targetNode = graph.Add(targetPose);
        var closestNeighbours = GetReachableNeighbours(graph, targetNode);
        if (closestNeighbours.Count == 0) { throw new NoPathException(); }
        graph.AddEdge(closestNeighbours[0], targetNode, CalcCost(closestNeighbours[0].value, targetPose));

        return FinalizePath(graph.GetPathToRoot(targetNode));
    }
}


public class GridRTTPathPlanner : IGridPlanner
{
    public static List<IPose> Path(IObstacleMap obstacleMap, IPose startPos, IPose targetPos, double timeOut = 10)
    {
        return (new RTTStar(obstacleMap)).FindPath(startPos, targetPos);
    }
}
