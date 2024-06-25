using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class GeneralHelpers
{
    public static List<Vector2> GenerateGridPointsInCircle(float cellWidth, float cellHeight, Vector2 center, float radius)
    {
        List<Vector2> points = new List<Vector2>();

        // Enumerate grid positions within the circle's bounding box
        for (float x = center.x - radius + cellWidth / 2f; x <= center.x + radius - cellWidth / 2f; x += cellWidth)
        {
            for (float y = center.y - radius + cellHeight / 2f; y <= center.y + radius - cellHeight / 2f; y += cellHeight)
            {
                // Check if the point is within the circle
                float distance = Vector2.Distance(center, new Vector2(x, y));
                if (distance <= radius)
                {
                    points.Add(new Vector2(x, y));
                }
            }
        }

        return points;
    }

    public static List<Vector2> GeneratePointsOnCircle(float cellWidth, Vector2 center, float radius)
    {
        List<Vector2> points = new List<Vector2>();

        // Calculate the angular step based on cell width and circle circumference
        float angleStep = cellWidth / (float)(2 * Math.PI * radius);    

        // Iterate through angles on the circle's circumference
        for (float angle = 0; angle < 2 * Math.PI; angle += angleStep)
        {
            // Calculate point coordinates on the circle using sine and cosine
            float x = center.x + radius * (float)Math.Cos(angle);
            float y = center.y + radius * (float)Math.Sin(angle);

            points.Add(new Vector2(x, y));
        }

        return points;
    }

    public static double DistanceFunc2D(float[] a, float[] b)
    {
        return Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2));
    }

    public static Vector2 Vec3ToVec2(Vector3 vec3)
    {
        return new Vector2(vec3.x, vec3.z);
    }

    public static Vector3 Vec2ToVec3(Vector2 vec3)
    {
        return new Vector3(vec3.x, 0, vec3.y);
    }

    public static bool IsPointInTriangle(Vector2 point, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        // Compute vectors
        Vector2 v2v1 = v2 - v1;
        Vector2 v3v1 = v3 - v1;
        Vector2 pv1 = point - v1;

        // Compute dot products
        float dot00 = Vector2.Dot(v3v1, v3v1);
        float dot01 = Vector2.Dot(v3v1, v2v1);
        float dot02 = Vector2.Dot(v3v1, pv1);
        float dot11 = Vector2.Dot(v2v1, v2v1);
        float dot12 = Vector2.Dot(v2v1, pv1);

        // Compute barycentric coordinates
        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // Check if point is in triangle
        return (u >= 0) && (v >= 0) && (u + v < 1);
    }

    public static Vector2 RotateAroundOrigin(Vector2 v, float rad)
    {
        // Calculate sine and cosine of the angle
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        // Perform rotation using sine and cosine
        float newX = v.x * cos - v.y * sin;
        float newY = v.x * sin + v.y * cos;

        // Return the rotated vector
        return new Vector2(newX, newY);
    }

    public static Vector2 RotateAroundPoint(Vector2 v, Vector2 pivot, float rad)
    {
        // Translate the vector to the origin
        Vector2 translatedVector = v - pivot;

        // Calculate sine and cosine of the angle
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        // Perform rotation using sine and cosine
        float newX = translatedVector.x * cos - translatedVector.y * sin;
        float newY = translatedVector.x * sin + translatedVector.y * cos;

        // Translate the vector back to the pivot point
        Vector2 rotatedVector = new Vector2(newX, newY) + pivot;

        // Return the rotated vector
        return rotatedVector;
    }

    /// @return transformation matrix for rotation around y by @p angle in rad and then @p translation
    public static Matrix4x4 CreateTransformationMatrix(float angle, Vector3 translation)
    {
        // Create rotation matrix around Y axis
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);

        // Create translation matrix
        Matrix4x4 translationMatrix = Matrix4x4.Translate(translation);

        // Combine rotation and translation matrices
        Matrix4x4 finalMatrix = translationMatrix * rotationMatrix;

        return finalMatrix;
    }

    public static T FindMinWithCustomComparer<T>(IEnumerable<T> list, Func<T, T, int> comparer)
    {
        T min = list.First();

        if (min == null)
        {
            throw new ArgumentException("List is empty");
        }

        foreach (T t in list)
        {
            if (comparer(t, min) < 0)
            {
                min = t;
            }
        }

        return min;
    }


    public static List<Vector2> LerpedVecs(Vector2 from, Vector2 to, int numSteps)
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

    // give sampling distance to hit start and target point with maxInterval
    public static double CalcualteSamplingDistance(double distance, double maxInterval)
    {
        // Calculate the number of intervals based on the maximum interval
        int numIntervals = (int)Math.Floor(distance / maxInterval);

        // If only one interval is possible, return the maximum interval
        if (numIntervals <= 1)
        {
            return maxInterval;
        }

        // Adjust the number of intervals to include start and target
        numIntervals += 1;

        // Calculate the actual interval considering including start and target
        double actualInterval = distance / numIntervals;
        return actualInterval;
    }
}