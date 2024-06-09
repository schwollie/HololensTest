using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SimpleMap : IObstacleMap
{
    Texture2D map;
    float NumPixelsPerMeter;
    Vector2 size;

    float[,] distanceMap;
    static float distanceMapResolution = 0.2f;

    private static int DebugPrintMaxCols = 60;
    static float MinResolution = 0.2f;

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

        GenerateDistanceMap();
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
                distanceMap[x, y] = 100;
                Vector2 pos = new Vector2(x*distanceMapResolution, y*distanceMapResolution);
                for (float r = 0; r < 2; r+=Resolution())
                {
                    if (!IsFree(new Vector2(pos.x+r, pos.y+ r)) || !IsFree(new Vector2(pos.x - r, pos.y + r)) || !IsFree(new Vector2(pos.x + r, pos.y - r)) || !IsFree(new Vector2(pos.x - r, pos.y - r)))
                    {
                        distanceMap[x, y] = r;
                        break;
                    }
                }
            }
        }
    }

    public float Resolution()
    {
        return 1 / NumPixelsPerMeter;
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
        return IsFree((int)(xy.x * NumPixelsPerMeter), (int)(xy.y * NumPixelsPerMeter));
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
        return DistanceToObstacle((int)(xy.x / distanceMapResolution), (int)(xy.y / distanceMapResolution));
    }

    static bool IsAlmostWhite(Color color)
    {
        float threshold = 0.8f; // Adjust threshold for color tolerance (0.0 - 1.0)
        return color.r >= threshold && color.g >= threshold && color.b >= threshold;
    }

    public void PrintMap(List<IPose> path = null)
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
                    foreach (IPose p in path)
                    {
                        if ((p.GetPos() - new Vector2(x / NumPixelsPerMeter, y / NumPixelsPerMeter)).magnitude < (map.width / (NumPixelsPerMeter * DebugPrintMaxCols)))
                        {
                            c = "◆";
                        }
                    }
                }


                row += c;
            }
            Debug.Log(row);
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
