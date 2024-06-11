using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
