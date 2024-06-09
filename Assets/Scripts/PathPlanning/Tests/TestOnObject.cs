using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class TestOnObject : MonoBehaviour
{
    SimpleMap map;
    private void Start()
    {
        //map = MapLoader.LoadMap("tests/VeryLargeMap");
    }
    void Update()
    {
        //var path = GridAStarPlanner.Path(map, new DefaultPose(3, 1, 0), new DefaultPose(5, 500, 0), 20);
    }
}
