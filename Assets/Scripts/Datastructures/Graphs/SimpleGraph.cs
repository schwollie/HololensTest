using KdTree.Math;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Profiling;

public class Edge<T>
{
    public readonly Node<T> nodeFrom;
    public readonly Node<T> nodeTo;
    public readonly float cost;

    public Edge(Node<T> from, Node<T> to, float cost)
    {
        this.nodeFrom = from;
        this.nodeTo = to;
        this.cost = cost;

    }

    public void Remove()
    {
        nodeFrom.RemoveEdge(this);
        nodeTo.RemoveEdge(this);
    }
}

public class Node<T>
{
    public Edge<T> predecessorEdge = null;
    public readonly List<Edge<T>> edgesSuccessor = new List<Edge<T>>();
    public readonly T value;

    private float cost = float.PositiveInfinity;

    public Node(T value) { this.value = value; }

    public void RemoveEdge(Edge<T> edge)
    {
        if (edge.nodeFrom == this)
        {
            edgesSuccessor.Remove(edge);
        }
        else { predecessorEdge = null; }
    }

    public void Remove()
    {
        if (predecessorEdge != null) { predecessorEdge.Remove(); }
        foreach (var edge in edgesSuccessor) { edge.Remove(); }
    }

    public float GetCost()
    {
        return cost;
    }

    public void UpdateCost(float cost)
    {
        this.cost = cost;
        foreach (Edge<T> edge in edgesSuccessor)
        {
            edge.nodeTo.UpdateCost(this.cost + edge.cost);
        }
    }
}

public interface ITree<T> {
    public Node<T> Add(T value);

    public IEnumerable<Node<T>> RandomSubSample(int maxLength);
    
    public void Remove(Node<T> node);

    public void AddEdgeOverride(Node<T> from, Node<T> to, float cost);

    public delegate float GetCost(T a, T b);
    public Node<T> GetClosest(T value, GetCost costFunc);
    public List<Node<T>> Neighbours(T value, float maxDistance, GetCost costFunc);
}



public class FastTree<T> : ITree<T> where T : IPose
{
    KdTree.KdTree<float, Node<T>> tree = new(2, new FloatMath());

    public KdTree.KdTree<float, Node<T>> Tree() { return tree; }

    private float[] ToArray(IPose vec)
    {
        return new float[] { vec.GetPos().x, vec.GetPos().y };
    }

    public Node<T> Add(T value)
    {
        Profiler.BeginSample("Add Node");
        Node<T> node = new(value);
        tree.Add(ToArray(value), node);
        Profiler.EndSample();
        return node;
    }

    public IEnumerable<Node<T>> RandomSubSample(int maxLength)
    {
        Profiler.BeginSample("Sub sample");
        List<Node<T>> sample = new List<Node<T>>();
        var toBeExtendedNodes = RandomHelper.ReservoirSampleIndices(tree.Count, maxLength);
        foreach (var index in toBeExtendedNodes)
        {
            sample.Add(tree.ElementAt(index).Value);
        }
        Profiler.EndSample();
        return sample;
    }

    public void Remove(Node<T> node)
    {
        Profiler.BeginSample("Remove Node");
        node.Remove();
        tree.RemoveAt(ToArray(node.value));
        Profiler.EndSample();
    }

    public void AddEdgeOverride(Node<T> from, Node<T> to, float cost)
    {
        Profiler.BeginSample("Add Edge");
        if (from == null || to == null) { throw new System.Exception("No node found"); }
        if (from == to) { 
            throw new System.Exception("Equal nodes is prohibited for edge."); 
        }

        if (to.predecessorEdge != null)
        {
            to.predecessorEdge.Remove();
        }

        var newEdge = new Edge<T>(from, to, cost);
        from.edgesSuccessor.Add(newEdge);
        to.predecessorEdge = newEdge;
        to.UpdateCost(from.GetCost() + cost);
        Profiler.EndSample();
    }

    /// @return the closest node
    public Node<T> GetClosest(T value, ITree<T>.GetCost costFunc)
    {
        Profiler.BeginSample("Clostest Node");
        var nearest = tree.GetNearestNeighbours(ToArray(value), 1);
        if (nearest.Length == 0) {  throw new System.Exception("No nearest node found"); }
        Profiler.EndSample();
        return nearest[0].Value;
    }

    public List<Node<T>> Neighbours(T value, float maxDistance, ITree<T>.GetCost costFunc)
    {
        var neighbours = tree.RadialSearch(ToArray(value), maxDistance).Where(n => n.Value.value.GetPos() != value.GetPos()).Select(n => n.Value).ToList();

        neighbours.Sort((Node<T> c1, Node<T> c2) =>
        {
            return costFunc(value, c1.value).CompareTo(costFunc(value, c2.value));
        });

        return neighbours;
    }

    public static List<Node<T>> GetPathToRoot(Node<T> node)
    {
        List<Node<T>> pathToRoot = new List<Node<T>>();

        Node<T> currentNode = node;
        while (currentNode != null)
        {
            pathToRoot.Add(currentNode);
            if (currentNode.predecessorEdge == null) { break; }
            currentNode = currentNode.predecessorEdge.nodeFrom;
        }

        return pathToRoot;
    }
}

public class SimpleTree<T> : ITree<T> where T : IPose
{
    public readonly List<Node<T>> nodes = new List<Node<T>>();

    public Node<T> Add(T value)
    {
        Node<T> newNode = new Node<T>(value);
        nodes.Add(newNode);
        return newNode;
    }

    public void Remove(Node<T> node)
    {
        node.Remove();
        nodes.Remove(node);
    }

    public IEnumerable<Node<T>> RandomSubSample(int maxLength)
    {
        return RandomHelper.ReservoirSample(nodes, maxLength);
    }

    public void AddEdge(Node<T> from, Node<T> to, float cost)
    {
        if (to.predecessorEdge != null)
        {
            throw new System.Exception("Tree node does only support one predecessor");
        }
        var newEdge = new Edge<T>(from, to, cost);
        from.edgesSuccessor.Add(newEdge);
        to.predecessorEdge = newEdge;
        to.UpdateCost(from.GetCost() + cost);
    }

    public void AddEdgeOverride(Node<T> from, Node<T> to, float cost)
    {
        if (to.predecessorEdge != null)
        {
            to.predecessorEdge.Remove();
        }
        AddEdge(from, to, cost);
    }

    /// @return the closest node
    public Node<T> GetClosest(T value, ITree<T>.GetCost costFunc)
    {
        // Initialize variables for closest node and distance
        Node<T> closestNode = null;
        double closestDistance = double.MaxValue;

        // Loop through all nodes
        foreach (Node<T> node in nodes)
        {
            if (!node.value.Equals(value))
            {
                double distance = costFunc(node.value, value);
                // Update closest node and distance
                if (distance < closestDistance)
                {
                    closestNode = node;
                    closestDistance = distance;
                }
            }
        }

        return closestNode;
    }
    /// @return neighbour nodes in a given range @p maxDistance in ascending order of the distance determined by @p distanceFunc
    public List<Node<T>> Neighbours(T value, float maxDistance, ITree<T>.GetCost costFunc)
    {
        List<Node<T>> neighbours;
        if (nodes.Count() > 100)
        {
            neighbours = ParallelNeighbours(value, maxDistance);
        }
        else
        {
            neighbours = new List<Node<T>>();
            foreach (var _node in nodes)
            {
                if (value.GetPos() == _node.value.GetPos()) { continue; }
                if (MapDistance(value, _node.value) < maxDistance) { neighbours.Add(_node); }
            }
        }

        neighbours.Sort((Node<T> c1, Node<T> c2) =>
        {
            return costFunc(value, c1.value).CompareTo(costFunc(value, c2.value));
        });
        return neighbours;
    }

    public List<Node<T>> GetPathToRoot(Node<T> node)
    {
        List<Node<T>> pathToRoot = new List<Node<T>>();

        Node<T> currentNode = node;
        while (currentNode != null)
        {
            pathToRoot.Add(currentNode);
            if (currentNode.predecessorEdge == null) { break; }
            currentNode = currentNode.predecessorEdge.nodeFrom;
        }

        return pathToRoot;

    }

    private List<Node<T>> ParallelNeighbours(T value, float maxDistance)
    {
        var neighbours = new ConcurrentBag<Node<T>>();

        Parallel.ForEach(nodes, _node =>
        {
            if (value.GetPos() != _node.value.GetPos() && MapDistance(value, _node.value) < maxDistance) { neighbours.Add(_node); }
        });

        return neighbours.ToList();
    }
    public float MapDistance(Node<T> a, Node<T> b)
    {
        return MapDistance(a.value, b.value);
    }

    public float MapDistance(T a, T b)
    {
        return (a.GetPos() - b.GetPos()).magnitude;
    }

}
