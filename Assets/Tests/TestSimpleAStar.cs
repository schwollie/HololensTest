using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestAStarNoRotation
{

    /*[Test]
    public void StartIsTarget()
    {
        var map = MapLoader.LoadMap("tests/simpleMap");
        var path = GridAStarPlanner.Path(map, new DefaultPose(1, 1, 0), new DefaultPose(1, 1, 0));
        Assert.AreEqual(path.Count, 1);
        Assert.AreEqual(path[0].GetMapPos(), new Vector2Int(1, 1));
        map.PrintMap(path);
    }

    [Test]
    public void OneStep()
    {
        var map = MapLoader.LoadMap("tests/simpleMap");
        var path = GridAStarPlanner.Path(map, new DefaultPose(1, 1, 0), new DefaultPose(1, 2, 0));
        Assert.AreEqual(path.Count, 2);
        Assert.AreEqual(path[0].GetMapPos(), new Vector2Int(1, 1));
        Assert.AreEqual(path[1].GetMapPos(), new Vector2Int(1, 2));
        map.PrintMap(path);
    }

    [Test]
    public void Complex()
    {
        var map = MapLoader.LoadMap("tests/simpleMap");
        var path = GridAStarPlanner.Path(map, new DefaultPose(1, 1, 0), new DefaultPose(5, 6, 0));
        Assert.AreEqual(path.Count, 8);
        map.PrintMap(path);
    }

    [Test]
    public void NoPathAvailable()
    {
        var map = MapLoader.LoadMap("tests/LargeMap");
        map.PrintMap();
        Assert.Throws<NoPathException>(() => GridAStarPlanner.Path(map, new DefaultPose(1, 1, 0), new DefaultPose(5, 6, 0)));
        //Assert.AreEqual(path.Count, 7);
        //Assert.AreEqual(path[0].GetMapPos(), new Vector2Int(1, 1));
        //Assert.AreEqual(path[1].GetMapPos(), new Vector2Int(1, 2));
    }

    [Test]
    public void ComplexVisual()
    {
        var map = MapLoader.LoadMap("tests/LargeMap");
        map.PrintMap();
        var path = GridAStarPlanner.Path(map, new DefaultPose(3, 35, 0), new DefaultPose(1, 42, 0), 30);
        map.PrintMap(path);
    }

    [Test]
    public void VeryLargeVisual()
    {
        var map = MapLoader.LoadMap("tests/VeryLargeMap");
        var path = GridAStarPlanner.Path(map, new DefaultPose(3, 1, 0), new DefaultPose(5, 500, 0), 20);
        Assert.GreaterOrEqual(path.Count, 1000);
    }*/
}
