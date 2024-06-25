using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


public class TestSceneRTT : MonoBehaviour
{
    public GameObject agent;
    public MeshFilter agentBoundBox;
    public GameObject agentRotCenter;

    public GameObject target;
    public GameObject map;



    public Texture2D mapImage;
    public SimpleMap occupancyMap;

    private List<IConfiguration> path = new List<IConfiguration>();

    Vector3 startPos;
    double startRot;
    Vector3 targetPos;
    bool newRun = false;
    double targetRot;

    double time = 0;

    MotionModel model;

    // Start is called before the first frame update
    void Start()
    {
        SetMapTexture();
        occupancyMap = MapLoader.LoadMap(mapImage, new Vector2(this.gameObject.transform.localScale.x * 10, this.gameObject.transform.localScale.z * 10));

        startPos = agent.transform.position;
        startRot = agent.transform.eulerAngles.y;
        targetPos = target.transform.position;
        targetRot = target.transform.eulerAngles.y;

        target.transform.localScale =agent.transform.localScale;

        model = new MotionModel(agent.transform, agentBoundBox.GetComponent<MeshFilter>().mesh, GeneralHelpers.Vec3ToVec2(agentRotCenter.transform.localPosition));
    }

    void GeneratePath()
    {
        if (!newRun) { return; }
        if (Time.timeAsDouble - time < 3) { return; }
        time = Time.timeAsDouble;

        Debug.Log("GeneratePath");
        try
        {
            path = GridRTTPathPlanner.Path(occupancyMap, model, new SimpleConfiguration(startPos.x, startPos.z, (float)startRot * Mathf.Deg2Rad),
                new SimpleConfiguration(targetPos.x, targetPos.z, (float)targetRot * Mathf.Deg2Rad), 2);

        }
        catch (NoPathException e) { Debug.LogException(e); path = new List<IConfiguration>(); }

    }

    void FixedUpdate()
    {
        if ((startPos - agent.transform.position).magnitude > 0.1 ||
            (targetPos - target.transform.position).magnitude > 0.1 ||
            Mathf.Abs((float)(startRot - agent.transform.eulerAngles.y)) > 0.1 ||
            Mathf.Abs((float)(targetRot - target.transform.eulerAngles.y)) > 0.1)
        {
            startPos = agent.transform.position;
            startRot = agent.transform.eulerAngles.y;
            targetPos = target.transform.position;
            targetRot = target.transform.eulerAngles.y;
            newRun = true;
        }
        else { newRun = false; }
        GeneratePath();
        ShowPathFollowing();
    }

    private void SetMapTexture()
    {
        Material material = new Material(Shader.Find("Diffuse"));
        material.mainTexture = mapImage;
        map.GetComponent<Renderer>().material = material;
    }

    // Update is called once per frame
    void Update()
    {
        var lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(path[i].GetPos().x, 0.1f, path[i].GetPos().y));
        }
    }


    public float SpeedAnimation = 0.01f;
    private GameObject animationAgent;
    private float step = 0;
    void ShowPathFollowing()
    {
        // create animation gameobject if not created yet
        if (animationAgent == null)
        {
            // Create a new GameObject
            animationAgent = new GameObject("AnimationAgent");

            // Add a MeshFilter component to the new GameObject
            MeshFilter newMeshFilter = animationAgent.AddComponent<MeshFilter>();

            // Copy the mesh from the original MeshFilter to the new MeshFilter
            newMeshFilter.mesh = agentBoundBox.GetComponent<MeshFilter>().mesh;

            // Add a MeshRenderer component to the new GameObject (if needed)
            MeshRenderer originalMeshRenderer = agent.GetComponent<MeshRenderer>();
            if (originalMeshRenderer != null)
            {
                MeshRenderer newMeshRenderer = animationAgent.AddComponent<MeshRenderer>();
                newMeshRenderer.materials = originalMeshRenderer.materials;
            }

            animationAgent.transform.position = agent.transform.position;
            animationAgent.transform.rotation = agent.transform.rotation;
            animationAgent.transform.localScale = agent.transform.localScale;
        }

        // if path exists lerp trough it
        if (path.Count <= 1) { animationAgent.SetActive(false); return; }
        animationAgent.SetActive(true);
        try
        {
            int progress = (int)(step * path.Count) + 1;
            float nodeProgress = (step * path.Count + 1) - progress;

            List<IConfiguration> intermediate = model.IntermediatePoses(path[progress - 1], path[progress]);
            IConfiguration current = null;
            if (intermediate.Count > 0)
            {
                current = intermediate[(int)(nodeProgress * intermediate.Count)];
            }
            else
            {
                current = path[progress];
            }

            animationAgent.transform.position = GeneralHelpers.Vec2ToVec3(current.GetPos());
            animationAgent.transform.eulerAngles = new Vector3(0, Mathf.Rad2Deg * current.GetRotation(), 0);
        }
        catch (Exception)
        {

        }

        step += SpeedAnimation;
        if (step >= 1)
        {
            step = 0;
        }
    }
}
