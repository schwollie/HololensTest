using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using Codice.Client.BaseCommands;

public class MotionModel
{
    public static float DetectionPointSpacing = 0.1f;

    private Mesh mesh;
    private Transform meshTransform;

    public float maxBound { get; private set; }
    public float minBound { get; private set; }

    List<Vector2> pointsInBound = new(); // stores sampled points of mesh for collisison
                                         // check (no need to store bounds inside minBound)

    public float safetyMargin { get; private set; }

    public MotionModel(Transform meshTransform, Mesh mesh, Vector2 rotationCenter, float safetyMargin = 0.05f)
    {
        this.safetyMargin = safetyMargin;
        this.mesh = mesh;
        this.meshTransform = meshTransform;

        mesh.RecalculateBounds();

        maxBound = Mathf.Infinity;
        minBound = 0;
        CreatePoints(meshTransform.localScale);
        maxBound = FarthestPointOutside();
        minBound = FirstPointOutside();
        CreatePoints(meshTransform.localScale); // second time with minBound
    }

    private float FarthestPointOutside()
    {
        var farthestPoint = pointsInBound.Aggregate((a, b) => ((a.sqrMagnitude > (b).sqrMagnitude) ? a : b));
        return farthestPoint.magnitude;
    }

    private float FirstPointOutside()
    {
        float maxSpacing = Mathf.Sqrt(2 * DetectionPointSpacing * DetectionPointSpacing);
        for (float r = 0; r < maxBound; r += 0.1f)
        {
            for (float angle = 0; angle <= Math.PI * 2; angle += 0.1f)
            {
                Vector2 point = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);

                var closestPoint = pointsInBound.AsParallel().Aggregate((a, b) => ((point - a).sqrMagnitude < (point - b).sqrMagnitude ? a : b));
                if ((closestPoint - point).magnitude > maxSpacing)
                {
                    return r;
                }
            }
        }
        return 0;
    }

    private void CreatePoints(Vector3 scale)
    {
        pointsInBound.Clear();

        Bounds bounds = mesh.bounds;

        // Loop through x and y coordinates within the bounds
        for (float x = -scale.x; x <= scale.x; x += DetectionPointSpacing)
        {
            for (float y = -scale.z; y <= scale.z; y += DetectionPointSpacing)
            {
                // Create a Vector2 point
                Vector2 point = new Vector2(x, y);

                if (point.magnitude < minBound || point.magnitude > maxBound)
                {
                    // no need to create point inside this area
                    continue;
                }

                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    // Get the vertices of the triangle
                    Vector2 v1 = GeneralHelpers.Vec3ToVec2(mesh.vertices[mesh.triangles[i]]);
                    Vector2 v2 = GeneralHelpers.Vec3ToVec2(mesh.vertices[mesh.triangles[i + 1]]);
                    Vector2 v3 = GeneralHelpers.Vec3ToVec2(mesh.vertices[mesh.triangles[i + 2]]);

                    v1.Scale(GeneralHelpers.Vec3ToVec2(meshTransform.localScale));
                    v2.Scale(GeneralHelpers.Vec3ToVec2(meshTransform.localScale));
                    v3.Scale(GeneralHelpers.Vec3ToVec2(meshTransform.localScale));

                    if (GeneralHelpers.IsPointInTriangle(point, v1, v2, v3))
                    {
                        pointsInBound.Add(point);
                        break;
                    }
                }
            }
        }

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
        double distToObstacle = map.DistanceToObstacle(pose.GetPos());
        if (distToObstacle > maxBound + safetyMargin)
        {
            return false;
        }
        else if (distToObstacle <= minBound + safetyMargin)
        {
            return true;
        }

        foreach (var p in pointsInBound)
        {
            // rotation is clockwise in unity so use negative rotation
            var correctPoint = GeneralHelpers.RotateAroundOrigin(p, -pose.GetRotation());
            correctPoint += pose.GetPos();

            //Debug.DrawLine(GeneralHelpers.Vec2ToVec3(correctPoint), GeneralHelpers.Vec2ToVec3(correctPoint) + new Vector3(0.1f, 0.1f, 0.1f));
            if (map.DistanceToObstacle(correctPoint) < map.Resolution() + safetyMargin) { return true; }
        }

        // somewhere in between sample points
        return false;
    }

    public bool IsFree(IConfiguration pose, IObstacleMap map) { return !IntersectsMap(pose, map); }

    public bool IsReachable(IConfiguration from, IConfiguration to, IObstacleMap map)
    {
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
            //Vector2 randPos = RandomHelper.RandomPointOnCircle(rMax) + mid;
            Vector2 randPos = map.RandomPosOnMap();

            double distToObstacle = map.DistanceToObstacle(randPos);
            if (!map.IsFree(randPos) || distToObstacle < minBound)
            {
                continue;
            }

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
