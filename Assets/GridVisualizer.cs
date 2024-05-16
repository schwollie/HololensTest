using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    public Transform playerCam;
    public GridMap map;
    public Material walkableMaterial;
    public Material nonWalkableMaterial;
    public float maxNeighbourHeightOffsetUnwalkable = 0.05f;

    public int maxObjects = 1000;
    [Range(1, 10)]
    public int skipTiles = 1;

    private int numObjectsWidth;

    private Queue<GameObject> objects = new Queue<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        numObjectsWidth = (int)Mathf.Sqrt(maxObjects);
        StartCoroutine("DoGridUpdate");
    }

    // Update is called once per frame
    IEnumerator DoGridUpdate()
    {
        while (true) { 
            float xStart = (float)(playerCam.position.x - numObjectsWidth * skipTiles * map.tileWidth * 0.5);
            float zStart = (float)(playerCam.position.z - numObjectsWidth * skipTiles * map.tileWidth * 0.5);

            // ensure that we are always on the grid
            for (int x = 0; x < numObjectsWidth; x++)
            {
                for (int z = 0; z < numObjectsWidth; z++)
                {
                    var pos = map.PosAligned(new Vector2(xStart + x * map.tileWidth * skipTiles, zStart + z * map.tileWidth * skipTiles), skipTiles);
                    Tile tile = map.GetTile(pos);

                    Vector2 offset = new Vector2(map.tileWidth + 0.0001f, map.tileWidth + 0.0001f);
                    List<Tile> sourround = map.GetTiles(pos - offset, pos + offset);
                    bool walkable = true;
                    foreach (Tile t in sourround)
                    {
                        if (Mathf.Abs(t.height - tile.height) > maxNeighbourHeightOffsetUnwalkable)
                        {
                            walkable = false; break;
                        }
                    }

                    // Create a cube for each cell
                    GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);

                    cell.transform.parent = this.transform;
                    cell.transform.position = new Vector3(pos.x, tile.height, pos.y);
                    cell.transform.localScale = new Vector3(map.tileWidth * skipTiles, 0.1f, map.tileWidth * skipTiles);

                    objects.Enqueue(cell);

                    if (objects.Count > maxObjects)
                    {
                        var obj = objects.Dequeue();
                        Destroy(obj);
                    }

                    // Determine
                    //Random.InitState((int)(pos.x + pos.y));
                    //bool isWalkable = (Random.Range(0f, 1f) < 0.8f); // 80% chance of being walkable

                    // Assign color based on walkability
                    Renderer renderer = cell.GetComponent<Renderer>();
                    if (walkable)
                    {
                        renderer.material = walkableMaterial;
                    }
                    else
                    {
                        renderer.material = nonWalkableMaterial;
                    }
                }
                yield return new WaitForSeconds(0.01f);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}
