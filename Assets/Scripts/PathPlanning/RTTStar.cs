using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;


public class RTTStar
{
    MotionModel motionModel;// = new NoRotationMotionModel();
    IObstacleMap obstacleMap;

    public RTTStar(IObstacleMap obstacelMap, MotionModel model)
    {
        this.obstacleMap = obstacelMap;
        this.motionModel = model;
    }

    private float CalcCost(IConfiguration a, IConfiguration b, bool includeObstacles = true)
    {
        float distFac = 1; float rotFac = 1.0f; float nodeFac = 0f; float obstFac = 1.0f;

        float rotCost = Mathf.Abs(Mathf.DeltaAngle(Mathf.Rad2Deg * a.GetRotation(), Mathf.Rad2Deg * b.GetRotation())) / 180;
        rotCost = rotCost * rotCost;
        float distCost = (a.GetPos() - b.GetPos()).magnitude;

        float distToObstacle = float.MaxValue;
        if (includeObstacles)
        {
            foreach (var vec in GeneralHelpers.LerpedVecs(a.GetPos(), b.GetPos(), 5))
            {
                float d = obstacleMap.DistanceToObstacle(vec);
                distToObstacle = Mathf.Min(d, distToObstacle);
            }
        }

        float obstacleDistCost = obstFac / (distToObstacle + 0.1f); // clamp between 0 and 1
        if (!includeObstacles)
        {
            obstacleDistCost = 0;
        }

        //return distCost;
        return rotCost * rotFac + distCost * distFac + obstacleDistCost + nodeFac;
    }

    /// @return a valid random pose from a given pose. If no pose was found in @p numTrys then null is returned
    private IConfiguration NewValidPose(IConfiguration pose, float maxDist, int numTrys = 20)
    {
        for (int i = 0; i < numTrys; i++)
        {
            float xOffset = RandomHelper.GenerateRandomFloatBothSigns(0f, maxDist);
            float yOffset = RandomHelper.GenerateRandomFloatBothSigns(0f, maxDist);
            float rotOffset = RandomHelper.GenerateRandomFloatBothSigns(-Mathf.PI, Mathf.PI);

            IConfiguration newPose = new SimpleConfiguration(pose.GetPos().x + xOffset, pose.GetPos().y + yOffset, pose.GetRotation() + rotOffset);
            if (motionModel.IsFree(newPose, obstacleMap))
            {
                return newPose;
            }
        }

        return null;
    }

    /// @return a new valid random pose in a given radius around start or target. If no pose was found in @p numTrys then null is returned
    private List<IConfiguration> GenerateRandomPosesFromExistingNodes(List<IConfiguration> nodes, int maxNodes, float maxDist)
    {

        var toBeExtendedNodes = RandomHelper.ReservoirSample(nodes, maxNodes);
        var poses = new List<IConfiguration>();

        int newNodes = Mathf.Max(maxNodes / toBeExtendedNodes.Count, 1);

        foreach (var node in toBeExtendedNodes)
        {
            for (int i = 0; i < newNodes; i++)
            {
                IConfiguration newPose = NewValidPose(node, maxDist, 20);
                if (newPose == null) { continue; }
                poses.Add(newPose);
            }
        }

        return poses;
    }

    private List<IConfiguration> GenerateRandomPosesWholeMap(IConfiguration startPose, IConfiguration targetPose, int nodesPerSquareMeter)
    {

        List<IConfiguration> poses = new List<IConfiguration>();

        float rMax = (targetPose.GetPos() - startPose.GetPos()).magnitude * 4;
        Vector2 mid = (targetPose.GetPos() + startPose.GetPos()) / 2f;

        for (int i = 0; i < nodesPerSquareMeter; i++)
        {
            IConfiguration newPose = motionModel.NewRandomPoseInCircle(rMax, mid, obstacleMap);
            if (newPose == null) { continue; }
            poses.Add(newPose);
        }

        return poses;
    }

    /// @return ascending neighbour nodes given @p radius around @p node. List is sorted by cost.
    private List<Node<IConfiguration>> GetReachableNeighbours(ITree<IConfiguration> graph, IConfiguration pose, Node<IConfiguration> target, float radius = 1.5f, int limit = 40)
    {
        List<Node<IConfiguration>> neighbours = graph.Neighbours(pose, radius);

        neighbours.Sort((c1, c2) =>
        {
            return (c1.GetCost() + CalcCost(pose, c1.value)).CompareTo(c2.GetCost() + CalcCost(pose, c2.value));
        });

        if (neighbours.Contains(target))
        {
            neighbours.Insert(0, target);
        }

        List<Node<IConfiguration>> reachableNeighbours = new();
        while (neighbours.Count > 0 && reachableNeighbours.Count < limit)
        {
            var n = neighbours.First();
            if (motionModel.IsReachable(n.value, pose, obstacleMap))
            {
                reachableNeighbours.Add(n);
            }
            neighbours.Remove(n);
        }
        return reachableNeighbours;
    }

    /// RTT* with heuristic (static number of nodes for performance sace)
    public List<IConfiguration> FindPath(IConfiguration startPose, IConfiguration targetPose, int nodesPerSquareMeter = 1000)
    {

        var startNode = new Node<IConfiguration>(startPose);
        var targetNode = new Node<IConfiguration>(targetPose);

        startNode.UpdateCost(0);

        List<Node<IConfiguration>> allNodes = GenerateRandomPosesWholeMap(startPose, targetPose, nodesPerSquareMeter).Select(n => new Node<IConfiguration>(n)).ToList();
        Debug.Log(allNodes.Count);

        HashSet<Node<IConfiguration>> discoveredNodes = new();
        HashSet<Node<IConfiguration>> toDiscover = new();
        toDiscover.Add(startNode);

        allNodes.Add(startNode);
        allNodes.Add(targetNode);
        ITree<IConfiguration> graph = new FastTree<IConfiguration>(allNodes);

        while (toDiscover.Count > 0)
        {
            var nodeToDiscover = toDiscover.AsParallel().Aggregate((a,b) => 
            (a.GetCost() + CalcCost(a.value, targetPose)).CompareTo(b.GetCost() + CalcCost(b.value, targetPose)) < 0 ? a : b);
            // minimal node with heuristic

            bool worked = toDiscover.Remove(nodeToDiscover);
            if (!worked) { 
                throw new Exception("");
            }

            var neighbours = GetReachableNeighbours(graph, nodeToDiscover.value, targetNode);
            // neighbours need to be sorted by the cost from nodeToDiscover to neighbour + neighbour.cost()
            if (neighbours.Count == 0) { continue; } // no neighbours can reach new node so continue

            if (nodeToDiscover != startNode)
            {
                var shortestNeighbourList = neighbours.Where(n => discoveredNodes.Contains(n)).ToList();
                var closest = shortestNeighbourList.FirstOrDefault();
                if (closest == null) { 
                    continue; 
                }
                graph.AddEdgeOverride(closest, nodeToDiscover, CalcCost(closest.value, nodeToDiscover.value));
                neighbours.Remove(closest);
            }

            discoveredNodes.Add(nodeToDiscover);

            foreach (var neighbour in neighbours)
            {
                if (!discoveredNodes.Contains(neighbour))
                {
                    toDiscover.Add(neighbour);

                }
                float edgeCost = CalcCost(nodeToDiscover.value, neighbour.value);
                if (nodeToDiscover.GetCost() + edgeCost < neighbour.GetCost())
                {
                    // will override predecessor and thereby remain tree structure
                    graph.AddEdgeOverride(nodeToDiscover, neighbour, edgeCost);
                }
            }

            if (targetNode.predecessorEdge != null) { break; }
        }


        foreach (var node in graph.Neighbours(startPose, 100))
        {
            //if (node.edgesSuccessor.Count == 0) { continue; }
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.Destroy(cube, 5f);
            cube.transform.position = new Vector3(node.value.GetPos().x, .1f, node.value.GetPos().y);
            cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.2f);
            cube.transform.Rotate(new Vector3(0,Mathf.Rad2Deg * node.value.GetRotation(),0)); 
            cube.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
        }

        Debug.Log("Explored :" + discoveredNodes.Count);

        // finally check if path was found
        if (targetNode.predecessorEdge == null) { throw new NoPathException(); }

        var path = FinalizePath(ITree<IConfiguration>.GetPathToRoot(targetNode));
        var refined = RefinePath(path, 1.5f, 150);
        refined = RefinePath(refined, 0.5f, 150);
        refined = RefinePath(refined, 0.1f, 150);
        refined = RefinePath(refined, 0f, 150);
        return refined;
    }

    // informed RTT* part (dynamic nodes)
    public List<IConfiguration> RefinePath(List<IConfiguration> path, float maxDist, int maxNodes = 200)
    {
        if (path.Count <= 2) { return path; }
        List<IConfiguration> toExplore = new();

        IConfiguration start = path.First();
        IConfiguration target = path.Last();
        path.Remove(start);
        path.Remove(target);

        toExplore.AddRange(path);
        toExplore.AddRange(GenerateRandomPosesFromExistingNodes(path, maxNodes, maxDist));

        ITree<IConfiguration> graph = new SimpleTree<IConfiguration>();
        var startNode = graph.Add(start);
        startNode.UpdateCost(0);

        while (toExplore.Count > 0)
        {
            Node<IConfiguration> nodeToDiscover = graph.Add(toExplore.First());
            toExplore.RemoveAt(0);

            var neighbours = GetReachableNeighbours(graph, nodeToDiscover.value, null, 1.5f, 100);
            // neighbours need to be sorted by the cost from nodeToDiscover to neighbour + neighbour.cost()
            if (neighbours.Count == 0) { graph.Remove(nodeToDiscover); continue; } // no neighbours can reach new node so continue

            var closest = neighbours.FirstOrDefault();
            if (closest == null)
            {
                graph.Remove(nodeToDiscover);
                continue;
            }
            graph.AddEdgeOverride(closest, nodeToDiscover, CalcCost(closest.value, nodeToDiscover.value));
            neighbours.Remove(closest);

            foreach (var neighbour in neighbours)
            {
                float edgeCost = CalcCost(nodeToDiscover.value, neighbour.value);
                if (nodeToDiscover.GetCost() + edgeCost < neighbour.GetCost())
                {
                    // will override predecessor and thereby remain tree structure
                    graph.AddEdgeOverride(nodeToDiscover, neighbour, edgeCost);
                }
            }


            var neighbours__ = GetReachableNeighbours(graph, target, null, 3, 1000);
            if (neighbours__.Count > 0)
            {
                Debug.Log(neighbours__[0].GetCost());
            }
        }

        var targetNode = graph.Add(target);
        var neighbours_ = GetReachableNeighbours(graph, targetNode.value, targetNode, 3, 1000);

        if (neighbours_.Count == 0)
        {
            throw new NoPathException();
        }

        graph.AddEdgeOverride(neighbours_[0], targetNode, CalcCost(neighbours_[0].value, targetNode.value));

        return FinalizePath(ITree<IConfiguration>.GetPathToRoot(targetNode));
    }

    public List<IConfiguration> FinalizePath(List<Node<IConfiguration>> targetToStart)
    {
        List<IConfiguration> path = new List<IConfiguration>();
        targetToStart.Reverse();
        foreach (var node in targetToStart)
        {
            path.Add(node.value);
        }
        return path;
    }
}


public class GridRTTPathPlanner : IGridPlanner
{
    public static List<IConfiguration> Path(IObstacleMap obstacleMap, MotionModel model, IConfiguration startPos, IConfiguration targetPos, double timeOut = 10)
    {
        return (new RTTStar(obstacleMap, model)).FindPath(startPos, targetPos);
    }
}
