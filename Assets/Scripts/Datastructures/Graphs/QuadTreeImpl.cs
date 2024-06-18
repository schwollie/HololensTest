using System;
using System.Collections.Generic;
using System.Linq;
using Supercluster.KDTree;

public class FastTree<T> : ITree<T> where T : IConfiguration
{
    KDTree<float, Node<T>> tree;
    List<Node<T>> nodes = new();

    public FastTree(List<Node<T>> nodes)
    {
        this.nodes = nodes;
        CreateTree();
    }

    private void CreateTree()
    {
        float[][] points = new float[nodes.Count][];
        for (int i = 0; i < nodes.Count; i++)
        {
            points[i] = ToArray((IConfiguration)nodes[i].value);
        }
        this.tree = new(2, points, nodes.ToArray(), GeneralHelpers.DistanceFunc2D);
    }

    private float[] ToArray(IConfiguration vec)
    {
        return new float[] { vec.GetPos().x, vec.GetPos().y };
    }

    public List<Node<T>> AddAll(List<T> value) {

        List<Node<T>> newNodes = new();
        foreach (T newVal in value)
        {
            var newNode = new Node<T>(newVal);
            newNodes.Add(newNode);
            nodes.Add(newNode);
        }

        CreateTree();

        return newNodes;
    }

    public Node<T> Add(T value)
    {
        var node = new Node<T>(value);
        nodes.Add(node);
        CreateTree();
        return node;
    }

    public IEnumerable<Node<T>> RandomSubSample(int maxLength)
    {
        return null;
        /*List<Node<T>> sample = new List<Node<T>>();
        var toBeExtendedNodes = RandomHelper.ReservoirSampleIndices(tree.Count, maxLength);
        foreach (var index in toBeExtendedNodes)
        {
            sample.Add(tree.ElementAt(index).Value);
        }

        return sample;*/
    }

    public void Remove(Node<T> node)
    {
        /*node.Remove();
        tree.RemoveAt(ToArray(node.value));*/
    }

    public List<Node<T>> AllNodes() { return nodes; }

    public void AddEdgeOverride(Node<T> from, Node<T> to, float cost)
    {

        if (from == null || to == null) { throw new System.Exception("No node found"); }
        if (from == to)
        {
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
    }

    /// @return the closest node
    public Node<T> GetClosest(T value, ITree<T>.GetCost costFunc)
    {
        var nearest = tree.NearestNeighbors(ToArray(value), 1);
        if (nearest.Length == 0) { throw new System.Exception("No nearest node found"); }
        return nearest[0].Item2;
    }

    public List<Node<T>> Neighbours(T value, float maxDistance)
    {
        var neighbours = tree.RadialSearch(ToArray(value), maxDistance).ToList().Select(n => n.Item2).
            Where(n => n.value.GetPos() != value.GetPos()).ToList();

        return neighbours;
    }
}
