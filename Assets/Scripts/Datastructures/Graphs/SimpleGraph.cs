using PlasticGui.WorkspaceWindow.Locks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

public class SimpleTree<T>
{
    public readonly List<Node<T>> nodes = new List<Node<T>>();
    public readonly List<Edge<T>> edges = new List<Edge<T>>();

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

    public void AddEdge(Node<T> from, Node<T> to, float cost)
    {
        if (to.predecessorEdge != null)
        {
            throw new System.Exception("Tree node does only support one predecessor");
        }
        var newEdge = new Edge<T>(from, to, cost);
        from.edgesSuccessor.Add(newEdge);
        to.predecessorEdge = newEdge;
        edges.Add(newEdge);
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

    public void RemoveEdge(Edge<T> edge)
    {
        edges.Remove(edge);
        edge.Remove();
    }

    public delegate float CalcDistance(T a, T b);

    /// @return the closest node
    public Node<T> GetClosest(Node<T> node, CalcDistance distanceFunc)
    {
        // Initialize variables for closest node and distance
        Node<T> closestNode = null;
        double closestDistance = double.MaxValue;

        // Loop through all nodes
        foreach (Node<T> _node in nodes)
        {
            if (_node != node)
            {
                double distance = distanceFunc(_node.value, node.value);

                // Update closest node and distance
                if (distance < closestDistance)
                {
                    closestNode = _node;
                    closestDistance = distance;
                }
            }
        }

        return closestNode;
    }

    /// @return neighbour nodes in a given range @p maxDistance in ascending order of the distance determined by @p distanceFunc
    public List<Node<T>> Neighbours(Node<T> node, CalcDistance distanceFunc, double maxDistance)
    {
        List<Node<T>> neighbours;
        if (nodes.Count() > 1000)
        {
            neighbours = ParallelNeighbours(node, distanceFunc, maxDistance);
        }
        else
        {


            neighbours = new List<Node<T>>();
            foreach (var _node in nodes)
            {
                if (node == _node) { continue; }
                if (distanceFunc(_node.value, node.value) < maxDistance) { neighbours.Add(_node); }
            }
        }

        neighbours.Sort((Node<T> c1, Node<T> c2) =>
        {
            double distance1 = distanceFunc(node.value, c1.value);
            double distance2 = distanceFunc(node.value, c2.value);
            return distance1.CompareTo(distance2);
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

    private List<Node<T>> ParallelNeighbours(Node<T> node, CalcDistance distanceFunc, double maxDistance)
    {
        var neighbours = new ConcurrentBag<Node<T>>();

        Parallel.ForEach(nodes, _node =>
        {
            if (node != _node && distanceFunc(_node.value, node.value) < maxDistance) { neighbours.Add(_node); }
        });

        return neighbours.ToList();
    }

}
