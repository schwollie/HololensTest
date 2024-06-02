using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SimpleMap : IObstacleMap
{
    Texture2D map;
    float NumPixelsPerMeter;

    private static int DebugPrintMaxCols = 60;

    public SimpleMap(Texture2D map, float numPixelsPerMeter = 100)
    {
        this.map = map;
        this.NumPixelsPerMeter = numPixelsPerMeter;
    }
    private bool IsFree(int x, int y)
    {
        if (x < 0 || x >= map.width || y < 0 || y >= map.height)
        {
            return false; // Out of bounds
        }

        return IsAlmostWhite(map.GetPixel(x, y));
    }

    public float Resolution()
    {
        return 0.1f;
    }

    public bool IsFree(Vector2 xy)
    {
        return IsFree((int)(xy.x * NumPixelsPerMeter), (int)(xy.y * NumPixelsPerMeter));
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
    public static SimpleMap LoadMap(string filePath, float numPixelsPerMeter = 100)
    {

        Texture2D img = Resources.Load<Texture2D>(filePath);

        if (img == null)
        {
            throw new FileNotFoundException(filePath);
        }

        return new SimpleMap(img, numPixelsPerMeter);
    }

    public static SimpleMap LoadMap(Texture2D img, float numPixelsPerMeter = 100)
    {
        return new SimpleMap(img, numPixelsPerMeter);
    }
}
