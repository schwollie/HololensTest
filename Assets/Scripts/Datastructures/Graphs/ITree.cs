using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;

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

public class Node<T> : IComparable
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

    public void UpdateCost(float cost, Node<T> start = null)
    {
        if (start == this) { return; }
        if (start == null) { start = this; }
        this.cost = cost;

        foreach (Edge<T> edge in edgesSuccessor)
        {
            edge.nodeTo.UpdateCost(this.cost + edge.cost, start);
        }
    }

    public int CompareTo(object obj)
    {
        Node<T> c = (Node<T>)obj;
        return cost.CompareTo(c.cost);
    }
}

public interface ITree<T>
{
    public Node<T> Add(T value);

    public void AddAll(T value) { throw new NotImplementedException(""); }

    public IEnumerable<Node<T>> RandomSubSample(int maxLength);

    public void Remove(Node<T> node);

    public void AddEdgeOverride(Node<T> from, Node<T> to, float cost);

    public delegate float GetCost(T a, T b);
    public Node<T> GetClosest(T value, GetCost costFunc);
    public List<Node<T>> Neighbours(T value, float maxDistance, GetCost costFunc);


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
