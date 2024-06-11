using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using System.Collections;
using static UnityEngine.GraphicsBuffer;


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

        bool a = obstacleMap.IsFree(pose.GetPos());

        return a;
    }

    private List<Vector2> SampleVec(Vector2 from, Vector2 to, int numSteps)
    {
        List<Vector2> l = new List<Vector2>();
        for (int i = 0; i <= numSteps; i++)
        {
            float progress = numSteps == 0 ? 0 : (float)(i) / ((float)(numSteps));
            var lerpedVec = Vector2.Lerp(from, to, progress);
            l.Add(lerpedVec);
        }

        return l;
    }

    private float CalcCost(IPose a, IPose b)
    {

        float distFac = 1; float rotFac = 1; float nodeFac = 0.05f; float obstFac = 0.2f;

        float rotCost = Mathf.Abs(Mathf.DeltaAngle(Mathf.Rad2Deg * a.GetRotation(), Mathf.Rad2Deg * b.GetRotation())) / 180;
        float distCost = (a.GetPos() - b.GetPos()).sqrMagnitude;

        float distToObstacle = float.MaxValue;
        foreach (var vec in SampleVec(a.GetPos(), b.GetPos(), 5))
        {
            float d = obstacleMap.DistanceToObstacle(vec);
            distToObstacle = Mathf.Min(d, distToObstacle);
        }

        float obstacleDistCost = obstFac / (distToObstacle + 1); // clamp between 0 and 1

        return /*rotCost * rotFac +*/ distCost * distFac + obstacleDistCost /*+ nodeFac*/;
    }

    /// @return if reachable and distance from @p from to next obstacle
    private bool IsReachable(IPose from, IPose to)
    {

        int numSteps = (int)((from.GetPos() - to.GetPos()).magnitude / (obstacleMap.Resolution()));
        for (int i = 0; i <= numSteps; i++)
        {
            float progress = numSteps == 0 ? 0 : (float)(i) / ((float)(numSteps));
            var lerpedVec = Vector2.Lerp(from.GetPos(), to.GetPos(), progress);
            if (!obstacleMap.IsFree(lerpedVec))
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
            float xOffset = RandomHelper.GenerateRandomFloatBothSigns(0.2f, 2f);
            float yOffset = RandomHelper.GenerateRandomFloatBothSigns(0.2f, 2f);
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
    private IPose NewRandomPoseCircle(float rMax, Vector2 mid, int numTrys = 20)
    {

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

    private List<IPose> GenerateRandomPosesFromExistingNodes(ITree<IPose> tree, int maxNewNodesPerIt)
    {

        var toBeExtendedNodes = tree.RandomSubSample(maxNewNodesPerIt);
        var poses = new List<IPose>();
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

        float rMax = (targetPose.GetPos() - startPose.GetPos()).magnitude * 2;
        Vector2 mid = (targetPose.GetPos() + startPose.GetPos()) / 2f;

        for (int i = 0; i < maxNewNodesPerIt; i++)
        {
            IPose newPose = NewRandomPoseCircle(rMax, mid);
            if (newPose == null) { continue; }
            poses.Add(newPose);
        }

        return poses;
    }

    /// return a pose from @p from to @p to which is closest to @p to but reachable from @p from
    public IPose LinearReachablePoseSearch(IPose from, IPose to, float minDist = 0.2f, int maxNumSteps = 7)
    {
        Vector2 currentPosFrom = from.GetPos();


        IPose validPose = null;
        for (int i = 0; i < maxNumSteps; i++)
        {
            Vector2 dir = (to.GetPos() - currentPosFrom);
            Vector2 newPos = dir * (float)Math.Pow(0.5f, (maxNumSteps - i)) + currentPosFrom;
            IPose newPose = new DefaultPose(newPos, to.GetRotation());

            // Create new pose and check reachability
            if (!IsReachable(newPose, from))
            {
                if ((newPose.GetPos() - from.GetPos()).magnitude < minDist) { return null; }
                return validPose;
            }
            validPose = newPose;
            currentPosFrom = newPose.GetPos();
        }

        if ((validPose.GetPos() - from.GetPos()).magnitude < minDist) { return null; }
        return validPose;
    }

    /// @return ascending neighbour nodes given @p radius around @p node. List is sorted by cost.
    private List<Node<IPose>> GetReachableNeighbours(ITree<IPose> graph, IPose pose, float radius = 1f, int limit = 200)
    {
        List<Node<IPose>> neighboursAscendingDistance = graph.Neighbours(pose, radius, CalcCost);
        List<Node<IPose>> reachableNeighbours = new();
        while (neighboursAscendingDistance.Count > 0 && reachableNeighbours.Count < limit)
        {
            var n = neighboursAscendingDistance.First();
            if (IsReachable(n.value, pose))
            {
                reachableNeighbours.Add(n);
            }
            neighboursAscendingDistance.Remove(n);
        }
        return reachableNeighbours;
    }

    public List<IPose> FindPath(IPose startPose, IPose targetPose, int maxNodes = 500)
    {

        var startNode = new Node<IPose>(startPose);
        var targetNode = new Node<IPose>(targetPose);

        startNode.UpdateCost(0);

        List<Node<IPose>> allNodes = GenerateRandomPosesWholeMap(startPose, targetPose, maxNodes).Select(n => new Node<IPose>(n)).ToList();
        HashSet<Node<IPose>> discoveredNodes = new();
        SortedSet<Node<IPose>> toDiscover = new();
        toDiscover.Add(startNode);
        allNodes.Add(startNode);
        allNodes.Add(targetNode);
        ITree<IPose> graph = new FastTree<IPose>(allNodes);

        while (toDiscover.Count > 0)
        {
            var nodeToDiscover = toDiscover.First();
            toDiscover.Remove(toDiscover.First());
            discoveredNodes.Add(nodeToDiscover);

            var neighboursAscendingDistance = GetReachableNeighbours(graph, nodeToDiscover.value);
            if (neighboursAscendingDistance.Count == 0) { continue; } // no neighbours can reach new node so continue

            if (nodeToDiscover != startNode)
            {
                var closest = neighboursAscendingDistance.Where(n => discoveredNodes.Contains(n)).FirstOrDefault();
                if (closest == null) { continue; }
                graph.AddEdgeOverride(closest, nodeToDiscover, CalcCost(neighboursAscendingDistance[0].value, nodeToDiscover.value));
                neighboursAscendingDistance.Remove(closest);
            }

            foreach (var neighbour in neighboursAscendingDistance)
            {
                if (!discoveredNodes.Contains(neighbour))
                {
                    toDiscover.Add(neighbour);
                }
                float cost = CalcCost(nodeToDiscover.value, neighbour.value);
                if (nodeToDiscover.GetCost() + cost < neighbour.GetCost())
                {
                    bool wasAdded = toDiscover.Remove(neighbour);
                    // will override predecessor and thereby remain tree structure
                    graph.AddEdgeOverride(nodeToDiscover, neighbour, cost);
                    if (wasAdded)
                    {
                        toDiscover.Add(neighbour);
                    }
                }
            }
        }


        foreach (var node in graph.Neighbours(startPose, 100, CalcCost))
        {
            if (node.edgesSuccessor.Count == 0) { continue; }
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(node.value.GetPos().x, .1f, node.value.GetPos().y);
            cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            cube.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
        }

        // finaly add target node and check if path was found
        if (targetNode.predecessorEdge == null) { throw new NoPathException(); }

        return FinalizePath(ITree<IPose>.GetPathToRoot(targetNode));
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
}


public class GridRTTPathPlanner : IGridPlanner
{
    public static List<IPose> Path(IObstacleMap obstacleMap, IPose startPos, IPose targetPos, double timeOut = 10)
    {
        return (new RTTStar(obstacleMap)).FindPath(startPos, targetPos);
    }
}
