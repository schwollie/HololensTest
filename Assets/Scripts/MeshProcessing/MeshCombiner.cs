using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Copy meshes from children into the parent's Mesh.
// CombineInstance stores the list of meshes.  These are combined
// and assigned to the attached Mesh.

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ExampleClass : MonoBehaviour
{
    public Material material;
    [SerializeField] private List<MeshFilter> sourceMeshFilters;

    [ContextMenu("ComineMeshes")]
    public void Combine()
    {
        var oldPos = transform.position;
        var oldRot = transform.rotation;

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        CombineInstance[] combine = new CombineInstance[sourceMeshFilters.Count];

        int i = 0;
        while (i < sourceMeshFilters.Count)
        {
            combine[i].mesh = sourceMeshFilters[i].sharedMesh;
            combine[i].transform = sourceMeshFilters[i].transform.localToWorldMatrix;
            //combine[i].tr
            sourceMeshFilters[i].gameObject.SetActive(false);
            i++;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine, true, true);
        transform.GetComponent<MeshFilter>().sharedMesh = mesh;
        transform.gameObject.SetActive(true);

        transform.position = oldPos;
        transform.rotation = oldRot;
    }


    void Start()
    {
        
    }
}