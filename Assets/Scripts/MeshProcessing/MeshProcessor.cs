using System.Collections;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

using SpatialAwarenessHandler = Microsoft.MixedReality.Toolkit.SpatialAwareness.IMixedRealitySpatialAwarenessObservationHandler<Microsoft.MixedReality.Toolkit.SpatialAwareness.SpatialAwarenessMeshObject>;


public class MeshProcessor : MonoBehaviour, SpatialAwarenessHandler
{
    IMixedRealitySpatialAwarenessMeshObserver observer;

    public Transform MainCam;
    //public SurfaceMeshesToPlanes meshToPlaneComponent;
    public GridMap gridMap;

    public float tolerance = 1.0f;

    bool initialized = false;

    // Start is called before the first frame update
    void Start()
    {
        var spatialAwarenessService = CoreServices.SpatialAwarenessSystem;
        var dataProviderAccess = spatialAwarenessService as IMixedRealityDataProviderAccess;

        var meshObserverName = "OpenXR Spatial Mesh Observer";
        observer = dataProviderAccess.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>(meshObserverName);

        if (CoreServices.SpatialAwarenessSystem != null)
        {
            CoreServices.SpatialAwarenessSystem.RegisterHandler<SpatialAwarenessHandler>(this);
            Debug.Log("Start Listening Mesh");
        }

        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {

        if (observer == null)
        {
            Debug.LogWarning("NO observer!");
        }
        foreach (var meshObj in observer.Meshes.Values)
        {
            ProcessMesh(meshObj.Filter.mesh, true);
            initialized = true;
        }

        yield return new WaitForSeconds(1);
    }

    // Update is called once per frame
    void Update()
    {
        Init();

    }

    void ProcessMesh(Mesh mesh, bool OverwriteHeight = false)
    {

        /*Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get the vertices of the triangle
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            // Calculate the normal vector
            Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;

            float dotProduct = Vector3.Dot(normal, Vector3.forward) * Vector3.Dot(normal, Vector3.left);
            if (dotProduct > tolerance || dotProduct < -tolerance)
            {
                // vertex is not floor
                continue;
            }

            Vector3[] vecs = new Vector3[] { v1, v2, v3 };

            float maxVertexHeight = vecs.Aggregate(float.MinValue, (acc, v) => v.y > acc ? v.y : acc);
            float minX = vecs.Aggregate(float.MaxValue, (acc, v) => v.x < acc ? v.x : acc);
            float maxX = vecs.Aggregate(float.MinValue, (acc, v) => v.x > acc ? v.x : acc);
            float minZ = vecs.Aggregate(float.MaxValue, (acc, v) => v.z < acc ? v.z : acc);
            float maxZ = vecs.Aggregate(float.MinValue, (acc, v) => v.z > acc ? v.z : acc);


            if (maxVertexHeight >= MainCam.position.y || maxVertexHeight < MainCam.position.y - 2.1)
            {
                // only consider floor which must be under the cams perspective
                continue;
            }


            List<Tile> tiles = gridMap.GetTiles(new Vector2(minX, minZ), new Vector2(maxX, maxZ));

            if (tiles.Count > 10)
            {
                continue;
            }*/


        /*foreach (Tile tile in tiles)
        {
            // get all colliding meshes
            RaycastHit[] hits = Physics.RaycastAll(tile.worldPos, Vector3.up, MainCam.position.y - maxVertexHeight, 31);

            // Loop through all hits
            foreach (RaycastHit hit in hits)
            {
                // Access the collided mesh or object
                MeshRenderer meshRenderer = hit.collider.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.
                }
            }
        }*/

        //tiles.ForEach(tile => tile.height = (maxVertexHeight > tile.height || OverwriteHeight ? maxVertexHeight : tile.height));
        //Tile t = gridMap.GetTile(new Vector2(minX, minZ));
        //t.height = maxVertexHeight;
    }


    void OnMeshChange(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> data)
    {
        SpatialAwarenessMeshObject spatialObject = data.SpatialObject;
        if (spatialObject == null)
        {
            return;
        }

        //meshToPlaneComponent.MakePlanes();

        Mesh mesh = spatialObject.Filter.mesh;
        ProcessMesh(mesh);

    }

    public void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        OnMeshChange(eventData);
    }

    public void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        // do nothing
    }

    public void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        OnMeshChange(eventData);
    }
}
