using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using Codice.Client.BaseCommands;
using System.Collections.Concurrent;

public class MotionModel
{
    public static float DetectionPointSpacing = 0.07f;

    private Mesh mesh;
    private Transform meshTransform;

    ObstacleDetector oDetector;

    List<Vector2> pointsInBound = new(); // stores sampled points of mesh for collisison
                                         // check (no need to store bounds inside minBound)

    public float safetyMargin { get; private set; }

    public MotionModel(Transform meshTransform, Mesh mesh, Vector2 rotationCenter, float safetyMargin = 0.15f)
    {
        this.safetyMargin = safetyMargin;
        this.mesh = mesh;
        this.meshTransform = meshTransform;

        mesh.RecalculateBounds();
        CreatePoints(meshTransform.localScale);
        oDetector = new(pointsInBound, safetyMargin, DetectionPointSpacing);
        oDetector.DebugBounds(GeneralHelpers.CreateTransformationMatrix(0, Vector3.zero));
    }

    private void CreatePoints(Vector3 scale)
    {
        pointsInBound.Clear();

        ConcurrentBag<Vector2> points = new ConcurrentBag<Vector2>();

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            // Get the vertices of the triangle
            Vector2 v1 = GeneralHelpers.Vec3ToVec2(mesh.vertices[mesh.triangles[i]]);
            Vector2 v2 = GeneralHelpers.Vec3ToVec2(mesh.vertices[mesh.triangles[i + 1]]);
            Vector2 v3 = GeneralHelpers.Vec3ToVec2(mesh.vertices[mesh.triangles[i + 2]]);

            v1.Scale(GeneralHelpers.Vec3ToVec2(meshTransform.localScale));
            v2.Scale(GeneralHelpers.Vec3ToVec2(meshTransform.localScale));
            v3.Scale(GeneralHelpers.Vec3ToVec2(meshTransform.localScale));

            float minX = Mathf.Min(Mathf.Min(v1.x, v2.x), v3.x);
            float maxX = Mathf.Max(Mathf.Max(v1.x, v2.x), v3.x);
            float minY = Mathf.Min(Mathf.Min(v1.y, v2.y), v3.y);
            float maxY = Mathf.Max(Mathf.Max(v1.y, v2.y), v3.y);

            int xSteps = Mathf.FloorToInt((maxX - minX) / DetectionPointSpacing);
            int ySteps = Mathf.FloorToInt((maxX - minX) / DetectionPointSpacing);

            float d = 0.0001f;

            for (float x = minX; x < maxX; x+= DetectionPointSpacing/2)
            {
                x = Mathf.Clamp(x, minX+d, maxX-d);
                for (float y = minY; y < maxY; y+= DetectionPointSpacing/2)
                {
                    y = Mathf.Clamp(y, minY+d, maxY-d);

                    var point = new Vector2(x, y);

                    if (this.pointsInBound.Count > 0)
                    {
                        var closestPoint = this.pointsInBound.Aggregate(pointsInBound.First(),
                        (s, b) => (((s - point).sqrMagnitude < (b - point).sqrMagnitude) ? s : b));

                        if ((closestPoint - point).magnitude < DetectionPointSpacing)
                        {
                            continue;
                        }
                    }
                    

                    if (GeneralHelpers.IsPointInTriangle(point, v1, v2, v3))
                    {
                        pointsInBound.Add(point);
                        break;
                    }

                }
            }

        }

        Comparison<Vector2> compareByXY = (v1, v2) =>
        {
            // Compare X first, then Y if X is equal
            int compareX = v1.x.CompareTo(v2.x);
            return compareX != 0 ? compareX : v1.y.CompareTo(v2.y);
        };

        pointsInBound.Sort(compareByXY);

        if (pointsInBound.Count == 0) { pointsInBound.Add(Vector2.zero); }
    }

    /// <summary>
    /// return poses from pose a to pose b. This model assumes moving with rotation from first pose
    /// to target pose and rotating to target roation on target position
    /// </summary>
    /// <returns></returns>
    public List<IConfiguration> IntermediatePoses(IConfiguration from, IConfiguration to)
    {
        List<IConfiguration> poses = new();

        float angleDegFrom = Mathf.Rad2Deg * from.GetRotation();
        float angleDegTo = Mathf.Rad2Deg * to.GetRotation();

        float deltaAngleDeg = Mathf.DeltaAngle(angleDegTo, angleDegFrom);

        int numSteps = (int)((from.GetPos() - to.GetPos()).magnitude / DetectionPointSpacing);
        for (int i = 1; i < numSteps; i++)
        {
            float progress = numSteps == 0 ? 0 : (float)(i) / ((float)(numSteps));
            var lerpedVec = Vector2.Lerp(from.GetPos(), to.GetPos(), progress);

            float lerpedAngle = Mathf.LerpAngle(angleDegFrom, angleDegTo, progress) * Mathf.Deg2Rad;
            poses.Add(new SimpleConfiguration(lerpedVec.x, lerpedVec.y, lerpedAngle));
        }



        /*for (int i = 0; i < angleSteps; i++)
        {
            float progress = angleSteps == 0 ? 0 : (float)(i) / ((float)(angleSteps));
            float lerpedAngle = Mathf.LerpAngle(angleDegFrom, angleDegTo, progress) * Mathf.Deg2Rad;
            poses.Add(new SimpleConfiguration(to.GetPos(), lerpedAngle));
        }*/

        return poses;
    }

    public bool IntersectsMap(IConfiguration pose, IObstacleMap map)
    {
        //double distToObstacle = map.DistanceToObstacle(pose.GetPos());
        /*if (distToObstacle > maxBound + safetyMargin)
        {
            return false;
        }
        else if (distToObstacle <= minBound + safetyMargin)
        {
            return true;
        }

        //bool ret = false;
        foreach (var p in pointsInBound)
        {
            // rotation is clockwise in unity so use negative rotation
            var correctPoint = GeneralHelpers.RotateAroundOrigin(p, -pose.GetRotation());
            correctPoint += pose.GetPos();


            //Debug.DrawRay(GeneralHelpers.Vec2ToVec3(correctPoint),Vector3.up, Color.black, 5);
            //ret = ret | map.DistanceToObstacle(correctPoint) < map.Resolution() + safetyMargin;
            if (map.DistanceToObstacle(correctPoint) < map.Resolution() + safetyMargin) { return true; }
        }

        // somewhere in between sample points
        return false;*/

        var matrix = GeneralHelpers.CreateTransformationMatrix(-pose.GetRotation(), pose.GetPos());
       // oDetector.DebugBounds(matrix);
        return oDetector.DoesCollide(map, matrix);
    }

    public bool IsFree(IConfiguration pose, IObstacleMap map) { return !IntersectsMap(pose, map); }

    public bool IsReachable(IConfiguration from, IConfiguration to, IObstacleMap map)
    {
        float angleDegFrom = Mathf.Rad2Deg * from.GetRotation();
        float angleDegTo = Mathf.Rad2Deg * to.GetRotation();

        float deltaAngleDeg = Mathf.DeltaAngle(angleDegTo, angleDegFrom);

        if (Mathf.Abs(deltaAngleDeg) > 90) { return false; }

        foreach (var pose in IntermediatePoses(from, to))
        {
            if (IntersectsMap(pose, map))
            {
                return false;
            }
        }

        return true;
    }

    public IConfiguration NewRandomPoseInCircle(float rMax, Vector2 mid, IObstacleMap map, int numTrys = 20, int numRotTrys = 10)
    {

        for (int i = 0; i < numTrys; i++)
        {
            Vector2 randPos = map.RandomPosOnMap();

            double distToObstacle = map.DistanceToObstacle(randPos);

            for (int rotTry = 0; rotTry < numRotTrys; rotTry++)
            {
                float rot = RandomHelper.GenerateRandomFloatBothSigns(0, (float)(2 * Math.PI));

                IConfiguration newPose = new SimpleConfiguration(randPos.x, randPos.y, rot);

                if (IsFree(newPose, map))
                {
                    return newPose;
                }

            }
        }
        return null;
    }

    public IConfiguration NewRandomPoseInSquare(Vector2 pos, IObstacleMap map, int numTrys = 20, int numRotTrys = 10)
    {

        for (int i = 0; i < numTrys; i++)
        {
            Vector2 randPos = pos + new Vector2(RandomHelper.GenerateRandomFloat(0, 1), RandomHelper.GenerateRandomFloat(0, 1));

            double distToObstacle = map.DistanceToObstacle(randPos);
            /*if (!map.IsFree(randPos) || distToObstacle < minBound)
            {
                continue;
            }*/

            for (int rotTry = 0; rotTry < numRotTrys; rotTry++)
            {
                float rot = RandomHelper.GenerateRandomFloatBothSigns(0, (float)(2 * Math.PI));

                IConfiguration newPose = new SimpleConfiguration(randPos.x, randPos.y, rot);

                if (IsFree(newPose, map))
                {
                    return newPose;
                }

            }
        }
        return null;
    }

    /// <summary>
    /// normal angle in direction of the vehicle
    /// </summary>
    /// <returns></returns>
    public float NormalAngle()
    {
        return 0;
    }
}
