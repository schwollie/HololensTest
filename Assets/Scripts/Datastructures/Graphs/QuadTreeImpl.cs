using System;
using System.Collections.Generic;
using System.Linq;
using Supercluster.KDTree;

public class FastTree<T> : ITree<T> where T : IPose
{
    KDTree<float, Node<T>> tree;

    public FastTree(List<Node<T>> nodes)
    {
        float[][] points = new float[nodes.Count][];
        for (int i = 0; i < nodes.Count; i++)
        {
            points[i] = ToArray((IPose)nodes[i].value);
        }
        this.tree = new(2, points, nodes.ToArray(), (float[] a, float[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2)));
    }

    private float[] ToArray(IPose vec)
    {
        return new float[] { vec.GetPos().x, vec.GetPos().y };
    }

    public void AddAll(T value) { throw new NotImplementedException(""); }

    public Node<T> Add(T value)
    {
        /*Node<T> node = new(value);
        tree.Add(ToArray(value), node);

        return node;*/
        return null;
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

    public List<Node<T>> Neighbours(T value, float maxDistance, ITree<T>.GetCost costFunc)
    {
        var neighbours = tree.RadialSearch(ToArray(value), maxDistance).ToList().Select(n => n.Item2).
            Where(n => n.value.GetPos() != value.GetPos()).ToList();

        neighbours.Sort((Node<T> c1, Node<T> c2) =>
        {
            return costFunc(value, c1.value).CompareTo(costFunc(value, c2.value));
        });

        return neighbours;
    }
}
