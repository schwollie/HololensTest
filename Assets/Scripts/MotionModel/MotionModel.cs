

using System;
using System.Collections.Generic;
using UnityEngine;

public class MotionModelPose : DefaultPose
{
    public MotionModelPose(Vector2Int pos, int rotation) : base(pos.x, pos.y, rotation)
    {
    }

    public static Tuple<float, IPose> Create(float cost, Vector2Int pos, short rotation)
    {
        return new Tuple<float, IPose>(cost, new MotionModelPose(pos, rotation));
    }

    public static List<Tuple<float, IPose>> CreateFromMap(float[,,] costMap)
    {
        List<Tuple<float, IPose>> neighbourCosts = new List<Tuple<float, IPose>>() { };
        for (short rot = -1; rot <= 1; rot++)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    float cost = costMap[rot + 1, x + 1, y + 1];
                    if (cost <= 0)
                    {
                        continue;
                    }
                    neighbourCosts.Add(MotionModelPose.Create(cost, new Vector2Int(x, y), rot));
                }
            }
        }
        return neighbourCosts;
    }
}

public interface IMotionModel
{
    public List<Tuple<float, IPose>> NeighbourCosts() { return null; }

    public List<Vector2Int> Boundaries(int rotation) { return null; }

    public List<IPose> NeighbourPoses(IPose currentPose) { return null; }

    //public bool DoesCollide(IPose pose, IObstacleMap)
}

public class NoRotationMotionModel : IMotionModel
{
    float[,,] neighbourMap = new float[,,] { { { 0, 0, 0 },   // rotation -1
                                              { 0, 0, 0 },
                                              { 0, 0, 0 } },
                                            { { 1.4f, 1, 1.4f },  // rotatation 0
                                              { 1, 0, 1 },
                                              { 1.4f, 1, 1.4f } },
                                            { { 0, 0, 0 },  // rotatation +1
                                              { 0, 0, 0 },
                                              { 0, 0, 0 } }};

    float[,] occupancyGrid = new float[,] { { 1, 1, },
                                            { 1, 1,} };

    public List<Tuple<float, IPose>> neighbourCosts;

    public NoRotationMotionModel()
    {
        neighbourCosts = MotionModelPose.CreateFromMap(neighbourMap);
    }
    public List<Tuple<float, IPose>> NeighbourCosts()
    {
        return neighbourCosts;
    }

}