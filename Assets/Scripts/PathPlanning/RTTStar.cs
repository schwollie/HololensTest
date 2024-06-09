using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using System.Collections;


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
        
        float distFac = 1; float rotFac = 1; float nodeFac = 0.05f; float obstFac = 1;

        float rotCost = Mathf.Abs(Mathf.DeltaAngle(Mathf.Rad2Deg * a.GetRotation(), Mathf.Rad2Deg * b.GetRotation())) / 180;
        float distCost = (a.GetPos() - b.GetPos()).sqrMagnitude;

        float distToObstacle = float.MaxValue; 
        foreach (var vec in SampleVec(a.GetPos(), b.GetPos(), 5))
        {
            float d = obstacleMap.DistanceToObstacle(vec);
            distToObstacle = Mathf.Min(d, distToObstacle);
        }

        float obstacleDistCost = distToObstacle == 0 ? 1 : 1 / distToObstacle;
        
        return /*rotCost * rotFac +*/ distCost * distFac + obstacleDistCost * obstFac /*+ nodeFac*/;
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
    private IPose NewRandomPoseCircle(IPose start, IPose target, int numTrys = 20)
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
    private List<Node<IPose>> GetReachableNeighbours(ITree<IPose> graph, IPose pose, float radius = 3)
    {
        List<Node<IPose>> neighboursAscendingDistance = graph.Neighbours(pose, radius, CalcCost);
        neighboursAscendingDistance = neighboursAscendingDistance.Where(n => IsReachable(n.value, pose)).ToList();
        return neighboursAscendingDistance;
    }

    public List<IPose> FindPath(IPose startPose, IPose targetPose, int maxIterations = 50, int maxNewNodesPerIt = 200)
    {
        ITree<IPose> graph = new SimpleTree<IPose>();

        var startNode = graph.Add(startPose);
        startNode.UpdateCost(0);


        for (int it = 0; it < maxIterations; it++)
        {
            List<IPose> newPoses = GenerateRandomPosesWholeMap(startPose, targetPose, maxNewNodesPerIt);

            foreach (var randomPose in newPoses)
            {
                var closestNode = graph.GetClosest(randomPose, CalcCost);
                IPose newPose = LinearReachablePoseSearch(closestNode.value, randomPose);
                if (newPose == null) { continue; }
                var neighboursAscendingDistance = GetReachableNeighbours(graph, newPose);
                if (neighboursAscendingDistance.Count == 0) { continue; } // no neighbours can reach new node so continue

                var newNode = graph.Add(newPose);
                graph.AddEdgeOverride(neighboursAscendingDistance[0], newNode, CalcCost(neighboursAscendingDistance[0].value, newPose));
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

        foreach (var node in graph.Neighbours(startPose, 100, CalcCost))
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(node.value.GetPos().x, .1f, node.value.GetPos().y);
            cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            cube.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
        }

        // finaly add target node and check if path was found
        var targetNode = graph.Add(targetPose);
        var closestNeighbours = GetReachableNeighbours(graph, targetPose);
        if (closestNeighbours.Count == 0) { throw new NoPathException(); }
        graph.AddEdgeOverride(closestNeighbours[0], targetNode, CalcCost(closestNeighbours[0].value, targetPose));


        return FinalizePath(FastTree<IPose>.GetPathToRoot(targetNode));
    }
}


public class GridRTTPathPlanner : IGridPlanner
{
    public static List<IPose> Path(IObstacleMap obstacleMap, IPose startPos, IPose targetPos, double timeOut = 10)
    {
        return (new RTTStar(obstacleMap)).FindPath(startPos, targetPos);
    }
}
