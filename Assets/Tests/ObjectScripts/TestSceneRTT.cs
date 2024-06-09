using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


public class TestSceneRTT : MonoBehaviour
{
    public GameObject start;
    public GameObject target;
    public RectTransform map;

    public Texture2D mapImage;
    private SimpleMap occupancyMap;

    private List<IPose> path = new List<IPose>();

    bool _threadRunning;
    Thread _thread;

    Vector3 startPos;
    double startRot;
    Vector3 targetPos;
    bool newRun = false;
    double targetRot;

    double time = 0;

    // Start is called before the first frame update
    void Start()
    {
        SetMapTexture();
        occupancyMap = MapLoader.LoadMap(mapImage, new Vector2(map.localScale.x * 10, map.localScale.y * 10));

        startPos = start.transform.position;
        startRot = start.transform.rotation.y;
        targetPos = target.transform.position;
        targetRot = target.transform.rotation.y;
    }

    void GeneratePath()
    {

        if (!newRun) { return; }
        if (Time.timeAsDouble - time < 3) { return; }
        time = Time.timeAsDouble;

        Debug.Log("GeneratePath");
        try
        {
            path = GridRTTPathPlanner.Path(occupancyMap, new DefaultPose(startPos.x, startPos.z, (float)startRot),
                new DefaultPose(targetPos.x, targetPos.z, (float)targetRot), 2);
            
        }
        catch (NoPathException e) { Debug.LogException(e); path = new List<IPose>(); }

    }

    void FixedUpdate()
    {
        if ((startPos - start.transform.position).magnitude > 0.1 ||
            (targetPos - target.transform.position).magnitude > 0.1 ||
            Mathf.Abs((float)(startRot - start.transform.rotation.y)) > 0.1 ||
            Mathf.Abs((float)(targetRot - target.transform.rotation.y)) > 0.1)
        {
            startPos = start.transform.position;
            startRot = start.transform.rotation.y;
            targetPos = target.transform.position;
            targetRot = target.transform.rotation.y;
            newRun = true;
        }
        else { newRun = false; }
        GeneratePath();
    }

    private void SetMapTexture()
    {
        Material material = new Material(Shader.Find("Diffuse"));
        material.mainTexture = mapImage;
        map.gameObject.GetComponent<Renderer>().material = material;
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

    void OnDisable()
    {
        // If the thread is still running, we should shut it down,
        // otherwise it can prevent the game from exiting correctly.
        if (_threadRunning)
        {
            // This forces the while loop in the ThreadedWork function to abort.
            _threadRunning = false;

            // This waits until the thread exits,
            // ensuring any cleanup we do after this is safe. 
            _thread.Join();
        }

        // Thread is guaranteed no longer running. Do other cleanup tasks.
    }
}
