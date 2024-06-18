using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class RandomHelper
{
    static Random random = new Random(1);
    /// <summary>
    /// Generates a random sub sample of the given list. The sub sample will always be smaller or equal in size of the original list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="maxElements">The maximum number of elements in the sample.</param>
    /// <returns>random sample of the original list</returns>
    public static List<T> ReservoirSample<T>(List<T> list, int maxElements, System.Random random = null)
    {
        GetRandom(random, out random);

        if (list.Count <= 2) { return list; }

        // Implementation from Wikipedia https://en.wikipedia.org/wiki/Reservoir_sampling
        maxElements = Mathf.Min(maxElements, list.Count-1);
        var reservoir = list.GetRange(0, maxElements);

        // Weight initialization
        double weight = Mathf.Exp(Mathf.Log((float)random.NextDouble()) / maxElements);

        int i = maxElements;
        while (i <= list.Count)
        {
            // Increment index based on weight and random number
            int step = (int)Mathf.Floor(Mathf.Log((float)random.NextDouble()) / Mathf.Log((float)(1 - weight))) + 1;
            i += step;

            // Check if within list bounds
            if (i <= list.Count)
            {
                // Replace random element in reservoir
                int randomIndex = random.Next(maxElements);
                reservoir[randomIndex] = list[i - 1];

                // Update weight
                weight *= Mathf.Exp(Mathf.Log((float)random.NextDouble()) / maxElements);
            }
        }

        return reservoir;
    }

    /// <summary>
    /// Generates a random sub sample of the given size from a theoretical list. The sub sample will always be smaller or equal in size of the original list.
    /// </summary>
    /// <param name="count">The size of the theoretical list.</param>
    /// <param name="maxElements">The maximum number of elements in the sample.</param>
    /// <param name="random">The random number generator (optional).</param>
    /// <returns>A list of random indices representing the sub sample.</returns>
    public static List<int> ReservoirSampleIndices(int count, int maxElements, Random random = null)
    {
        GetRandom(random, out random);

        // Implementation from Wikipedia https://en.wikipedia.org/wiki/Reservoir_sampling
        maxElements = Mathf.Min(maxElements, count);
        var reservoir = new List<int>(maxElements);

        // Initialize reservoir with first maxElements elements
        for (int x = 0; x < maxElements; x++)
        {
            reservoir.Add(x);
        }

        // Weight initialization
        double weight = Mathf.Exp(Mathf.Log((float)random.NextDouble()) / maxElements);

        int i = maxElements;
        while (i <= count)
        {
            // Increment index based on weight and random number
            int step = (int)Mathf.Floor(Mathf.Log((float)random.NextDouble()) / Mathf.Log((float)(1 - weight))) + 1;
            i += step;

            // Check if within theoretical list bounds
            if (i <= count)
            {
                // Replace random element in reservoir
                int randomIndex = random.Next(maxElements);
                reservoir[randomIndex] = i - 1;

                // Update weight
                weight *= Mathf.Exp(Mathf.Log((float)random.NextDouble()) / maxElements);
            }
        }

        return reservoir;
    }

    public static int GenerateRandomInt(int min, int max, Random random = null)
    {
        GetRandom(random, out random);

        int num = random.Next(min, max);
        return num;
    }

    public static float GenerateRandomFloat(float min, float max, Random random = null)
    {
        GetRandom(random, out random);

        float randomNumber = (float)random.NextDouble() * (max - min) + min;
        return randomNumber;
    }

    public static float GenerateRandomFloatBothSigns(float min, float max, Random random = null)
    {
        GetRandom(random, out random);
        float randomNumber = GenerateRandomFloat(min, max, random);
        if (random.NextDouble() > 0.5)
        {
            return randomNumber * -1;
        }
        return randomNumber;
    }


    /// <returns>random point on the circle from uniform distribution</returns>
    public static UnityEngine.Vector2 RandomPointOnCircle(double radius, Random random = null)
    {
        GetRandom(random, out random);

        double rSample = Mathf.Sqrt((float)random.NextDouble()) * radius;
        double theta = random.NextDouble() * 2f * Mathf.PI;

        return new UnityEngine.Vector2((float)(rSample * Mathf.Cos((float)theta)), (float)(rSample * Mathf.Sin((float)theta)));
    }

    private static void GetRandom(in Random rIn, out Random rOut) { if (rIn == null) { rOut = random; } rOut = rIn; }
}
