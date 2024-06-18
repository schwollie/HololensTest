using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


public class ObstacleChcker : MonoBehaviour
{
    public GameObject mesh;
    public GameObject map;


    private SimpleMap occupancyMap;

    MotionModel model;

    // Start is called before the first frame update
    void Start()
    {
        model = new MotionModel(mesh.transform, mesh.GetComponent<MeshFilter>().mesh, new Vector2(0,0));
    }

    void Update()
    {
        occupancyMap = map.GetComponent<TestSceneRTT>().occupancyMap;

        if (model.IntersectsMap(new SimpleConfiguration(GeneralHelpers.Vec3ToVec2(mesh.transform.position), mesh.transform.rotation.eulerAngles.y * Mathf.Deg2Rad), occupancyMap))
        {
            mesh.GetComponent<Renderer>().material.color = Color.red;
        } else
        {
            mesh.GetComponent<Renderer>().material.color = Color.green;
        }
    }
}
