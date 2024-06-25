using Aglomera;
using Aglomera.Linkage;
using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using log4net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

class BoundTree
{
    BoundTree left = null;
    BoundTree right = null;
        
    Vector2 center;
    float maxBound;
    float minBound;

    public BoundTree(Vector2 center, float minBound, float maxBound)
    {
        this.center = center;
        this.minBound = minBound;
        this.maxBound = maxBound;
    }

    public void AddChilds(BoundTree left, BoundTree right) { this.left = left; this.right = right; }
    
    // takes transform to rotate and apply position offset
    public bool DoesCollide(IObstacleMap map, Matrix4x4 transform)
    {
        Vector3 transformedPosition = transform.MultiplyPoint3x4(center);

        float distance = map.DistanceToObstacle(transformedPosition);
        if (distance > maxBound) { return false; }
        if (distance < minBound) { return true; }
        if (left == null && right == null && distance < maxBound) { return true; }
        bool collides = false;
        if (left != null) { collides = left.DoesCollide(map, transform); }
        if (collides) { return true; }
        if (right != null) { collides = right.DoesCollide(map, transform); }
        return collides;
    }

    public void DebugDraw(Matrix4x4 transform)
    {
        Vector3 transformedPosition = transform.MultiplyPoint3x4(center);
        Debug.DrawRay(GeneralHelpers.Vec2ToVec3(transformedPosition), Vector3.up, Color.black, 15);

        if (left != null) { left.DebugDraw(transform); }
        if (right != null) { right.DebugDraw(transform); }
    }
}



public class ObstacleDetector
{
    class DissimilarityMetric : IDissimilarityMetric<Point>
    {
        public double Calculate(Point instance1, Point instance2) { return (instance1.vec-instance2.vec).magnitude; }
    }

    class Point : IComparable<Point>
    {
        public Vector2 vec { get; set; }

        public Point(Vector2 vec)
        {
            this.vec = vec;
        }

        public int CompareTo(Point other)
        {
            // Compare data points based on their X coordinate
            int xComparison = vec.x.CompareTo(other.vec.x);
            if (xComparison != 0)
            {
                return xComparison;
            }

            // If X coordinates are equal, compare Y coordinates
            return vec.y.CompareTo(other.vec.y);
        }

        public static List<Point> FromVec(List<Vector2> vecs)
        {
            List<Point> result = new List<Point>();
            foreach (var vec in vecs)
            {
                result.Add(new Point(vec));
            }
            return result;
        }

        public static List<Vector2> FromPoints(List<Point> points)
        {
            List<Vector2> result = new List<Vector2>();
            foreach (var point in points)
            {
                result.Add(point.vec);
            }
            return result;
        }
    }

    BoundTree bounds;

    public ObstacleDetector(List<Vector2> pointsInObject, float safetyMargin, float maxPointDistance)
    {
        bounds = BuildBoundTree(Point.FromVec(pointsInObject), safetyMargin, maxPointDistance);
    }

    public bool DoesCollide(IObstacleMap map, Matrix4x4 transform)
    {
        if (bounds == null)
        {
            return false;
        }
        return bounds.DoesCollide(map, transform);
    }

    public void DebugBounds(Matrix4x4 transform)
    {
        bounds.DebugDraw(transform);
    }

    BoundTree BuildBoundTree(List<Point> pointsInObject, float safetyMargin, float maxPointDistance)
    {
        if (pointsInObject.Count == 0) { return null; }
        var averagePos = pointsInObject.Aggregate(new Vector2(0, 0), (s, v) => s + v.vec) / (float)pointsInObject.Count;
        var farthestPoint = pointsInObject.Aggregate(pointsInObject.First().vec ,
            (s, b) => (((s - averagePos).sqrMagnitude > (b.vec -averagePos).sqrMagnitude) ? s : b.vec));
        float maxBound = (farthestPoint - averagePos).magnitude;
        float minBound = FirstPointOutside(pointsInObject, averagePos, maxBound, maxPointDistance);

        BoundTree tree = new(averagePos, minBound, maxBound + safetyMargin);
        if (maxBound < maxPointDistance)
        {
            return tree;
        }

        var clusters = Cluster2(pointsInObject);
        tree.AddChilds(BuildBoundTree(clusters.Item1, safetyMargin, maxPointDistance), 
            BuildBoundTree(clusters.Item2, safetyMargin, maxPointDistance));
        return tree;
    }

    Tuple<List<Point>, List<Point>> Cluster2(List<Point> points)
    {
        var metric = new DissimilarityMetric();
        var linkage = new CompleteLinkage<Point>(metric);
        var algorithm = new AgglomerativeClusteringAlgorithm<Point>(linkage);
        var clustering = algorithm.GetClustering(points.ToHashSet());
         
        foreach (var cluster in clustering)
        {
            if (cluster.Count == 2)
            {
                return new Tuple<List<Point>, List<Point>>(cluster[0].ToList(), cluster[1].ToList());
            }
        }

        // only 1 cluster
        foreach (var cluster in clustering)
        {
            if (cluster.Count == 1)
            {
                return new Tuple<List<Point>, List<Point>>(cluster[0].ToList(), new List<Point>{});
            }
        }

        // empty clusters
        return new Tuple<List<Point>, List<Point>>(new List<Point> { }, new List<Point> { });
    }

    private float FirstPointOutside(List<Point> points, Vector2 mid, float maxRadius, float maxPointDistance)
    {
        float maxSpacing = Mathf.Sqrt(2 * maxPointDistance * maxPointDistance) + 0.01f;
        for (float r = 0; r < maxRadius; r += maxPointDistance / 2)
        {
            for (float angle = 0; angle <= Math.PI * 2; angle += 0.01f)
            {
                Vector2 point = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r) + mid;

                var closestPoint = points.Aggregate(points.First().vec,
                    (s, b) => (((s - mid).sqrMagnitude < (b.vec - mid).sqrMagnitude) ? s : b.vec));
                if ((closestPoint - point).magnitude > maxSpacing)
                {
                    return r;
                }
            }
        }
        return 0;
    }
}
