using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObstacleMap
{
    bool IsFree(Vector2 pos);

    float DistanceToObstacle(Vector2 pos);

    float Resolution();

    Vector2 RandomPosOnMap();
}
