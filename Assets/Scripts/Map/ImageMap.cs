using Supercluster.KDTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class SimpleMap : IObstacleMap
{
    Texture2D map;
    float NumPixelsPerMeter;
    Vector2 size;

    KDTree<float, float[]> obstacles;

    private static int DebugPrintMaxCols = 60;
    static float MinResolution = 0.2f;

    float[,] distanceMap;
    float distanceMapResolution = 0.1f;

    public SimpleMap(Texture2D refereneMap, Vector2 size)
    {
        this.size = size;
        // Calculate the current resolution
       // float currentResolution = size.x / refereneMap.width;

        // Check if resizing is necessary based on minimum resolution
        /*if (currentResolution < MinResolution)
        {
            int newWidth = Mathf.CeilToInt(size.x / MinResolution);
            int newHeight = Mathf.CeilToInt(refereneMap.height * (float)newWidth / refereneMap.width);

            // Resize the texture using the Resize method
            this.map = Resize(refereneMap, newWidth, newHeight);
        } else {  this.map = refereneMap; }*/

        this.map = refereneMap;
        this.NumPixelsPerMeter = map.width / size.x;

        List<float[]> obstaclePixels = new List<float[]>();
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                if (!IsFree(x, y))
                {
                    obstaclePixels.Add(new float[] {((float)x)/map.width * size.x, ((float)y) / map.height* size.y });
                }
            }
        }
        
        this.obstacles = new(2, obstaclePixels.ToArray(), obstaclePixels.ToArray(), GeneralHelpers.DistanceFunc2D);
        GenerateDistanceMap();
    }

    public Vector2 RandomPosOnMap()
    {
        return new Vector2(RandomHelper.GenerateRandomFloat(0, size.x), RandomHelper.GenerateRandomFloat(0, size.y));
    }

    Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
    {
        RenderTexture rt = new RenderTexture(targetX, targetY, 12);
        RenderTexture.active = rt;
        rt.filterMode = FilterMode.Point;
        Graphics.Blit(texture2D, rt);
        Texture2D result = new Texture2D(targetX, targetY);
        result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
        result.Apply();
        return result;
    }

    void GenerateDistanceMap()
    {
        distanceMap = new float[(int)(size.x / distanceMapResolution), (int)(size.y / distanceMapResolution)];

        for (int x = 0; x < distanceMap.GetLength(0); x++)
        {
            for (int y = 0; y < distanceMap.GetLength(1); y++)
            {
                float[] pos = new float[] { (float)x/ distanceMap.GetLength(0) * size.x, (float)y / distanceMap.GetLength(1) * size.y };
                float distance = (float)GeneralHelpers.DistanceFunc2D(obstacles.NearestNeighbors(pos, 1).First().Item1, pos);
                distance = Mathf.Min(pos[0], Mathf.Min(pos[1], distance));
                distance = Mathf.Min(size.x-pos[0], Mathf.Min(size.y-pos[1], distance));
                if (distance < Resolution()) { distance = 0; }
                distanceMap[x, y] = distance;
            }
        }
    }

    public float Resolution()
    {
        return 1 / NumPixelsPerMeter;
    }

    public List<Vector2> SquareMeters()
    {
        List<Vector2> result = new();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                result.Add(new Vector2(x, y));
            }
        }

        return result;
    }

    private bool IsFree(int x, int y)
    {
        if (x < 0 || x >= map.width || y < 0 || y >= map.height)
        {
            return false; // Out of bounds
        }

        return IsAlmostWhite(map.GetPixel(x, y));
    }

    public bool IsFree(Vector2 xy)
    {
        return (xy.x > 0 && xy.x <= size.x && xy.y > 0 && xy.y <= size.y) && DistanceToObstacle(xy) > Resolution();
    }

    private float DistanceToObstacle(int x, int y)
    {
        if (x < 0 || x >= distanceMap.GetLength(0) || y < 0 || y >= distanceMap.GetLength(1))
        {
            return 0;
        }

        return distanceMap[x, y];
        
    }

    public float DistanceToObstacle(Vector2 xy)
    {
        /*float[] pos = new float[] { xy.x, xy.y};
        return (float)GeneralHelpers.DistanceFunc2D(obstacles.NearestNeighbors(pos, 1).First().Item1, pos);*/
        return DistanceToObstacle((int)(xy.x / size.x * distanceMap.GetLength(0)), (int)(xy.y / size.y * distanceMap.GetLength(1)));
    }

    static bool IsAlmostWhite(Color color)
    {
        float threshold = 0.9f; // Adjust threshold for color tolerance (0.0 - 1.0)
        return color.r >= threshold && color.g >= threshold && color.b >= threshold;
    }

    public void PrintMap(List<Vector2> path = null)
    {

        Vector2Int checks = new Vector2Int(Mathf.Max(1, map.width / DebugPrintMaxCols), Mathf.Max(1, map.height / DebugPrintMaxCols));

        // Print map bottom to top (matching loop order)
        for (int y = map.height - 1; y >= 0; y -= checks.y)
        {
            string row = "";
            for (int x = 0; x < map.width; x += checks.x)
            {
                bool isFree = true;
                for (int ix = 0; ix < checks.x; ix++)
                {
                    for (int iy = 0; iy < checks.x; iy++)
                    {
                        isFree &= IsFree(x + ix, y + iy);
                    }

                }

                string c = isFree ? "◇" : "◆";
                if (path != null)
                {
                    foreach (var p in path)
                    {
                        if ((p - new Vector2(x / NumPixelsPerMeter, y / NumPixelsPerMeter)).magnitude < (map.width / (NumPixelsPerMeter * DebugPrintMaxCols)))
                        {
                            c = "◆";
                        }
                    }
                }


                row += c;
            }
            UnityEngine.Debug.Log(row);
        }
    }
}

public class MapLoader
{
    public static SimpleMap LoadMap(string filePath, Vector2 size)
    {

        Texture2D img = Resources.Load<Texture2D>(filePath);

        if (img == null)
        {
            throw new FileNotFoundException(filePath);
        }

        return new SimpleMap(img, size);
    }

    public static SimpleMap LoadMap(Texture2D img, Vector2 size)
    {
        return new SimpleMap(img, size);
    }
}
