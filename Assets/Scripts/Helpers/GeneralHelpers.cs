using System;
using System.Collections.Generic;
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
}