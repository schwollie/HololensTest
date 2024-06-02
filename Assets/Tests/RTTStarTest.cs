using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RTTStarTest
{
    [Test]
    public void RTTStarTestSimplePasses()
    {
        var map = MapLoader.LoadMap("tests/PlayRoom1000x1000");
        var path = GridRTTPathPlanner.Path(map, new DefaultPose(1, 1, 0), new DefaultPose(2, 2f, 0), 100);
        map.PrintMap(path);
    }
}
