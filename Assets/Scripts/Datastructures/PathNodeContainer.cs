using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;

public struct Neighbour<Node>
{
    public float cost;
    public Node node;

    public Neighbour(float cost, Node node)
    {
        this.cost = cost;
        this.node = node;
    }
}

public class NodeStorage<Node> where Node : IPose, new()
{
    HashSet<IPose> nodes = new HashSet<IPose>();

    public int GetCount() { return nodes.Count; }
    public Node GetNode(IPose pose)
    {
        IPose outNode = pose;
        bool success = nodes.TryGetValue(pose, out outNode);
        if (!success)
        {
            Node n = new Node();
            n.Fill(pose);
            nodes.Add(n);
            return (Node)n;
        }
        return (Node)outNode;
    }

    /// return neighbours and the cost associated with reaching them
    public List<Neighbour<Node>> Neighbours(IPose currentPose, IMotionModel model)
    {
        List<Neighbour<Node>> neighbours = new List<Neighbour<Node>>();

        var motionNeighbours = model.NeighbourCosts();
        foreach (var mNode in motionNeighbours)
        {
            var node = GetNode(mNode.Item2.Aggregate(currentPose));
            neighbours.Add(new Neighbour<Node>(mNode.Item1, node));
        }


        return neighbours;
    }

}