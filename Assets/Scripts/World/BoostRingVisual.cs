using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public sealed class BoostRingVisual : MonoBehaviour
{
    private static Mesh ringMesh;

    private void Awake()
    {
        if (ringMesh == null) ringMesh = CreateRing();
        GetComponent<MeshFilter>().sharedMesh = ringMesh;
    }

    private static Mesh CreateRing()
    {
        const int ringSegments = 32;
        const int tubeSegments = 8;
        const float ringRadius = 0.69f;
        const float tubeRadius = 0.11f;
        var vertices = new Vector3[ringSegments * tubeSegments];
        var normals = new Vector3[vertices.Length];
        var indices = new int[vertices.Length * 6];
        int index = 0;
        for (int ring = 0; ring < ringSegments; ++ring)
        {
            float angle = ring * (2f * Mathf.PI / ringSegments);
            var radial = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));
            for (int tube = 0; tube < tubeSegments; ++tube)
            {
                float profile = tube * (2f * Mathf.PI / tubeSegments);
                int current = ring * tubeSegments + tube;
                normals[current] = radial * Mathf.Cos(profile) + Vector3.right * Mathf.Sin(profile);
                vertices[current] = radial * ringRadius + normals[current] * tubeRadius;

                int nextTube = (tube + 1) % tubeSegments;
                int nextRing = ((ring + 1) % ringSegments) * tubeSegments;
                indices[index++] = current;
                indices[index++] = nextRing + tube;
                indices[index++] = ring * tubeSegments + nextTube;
                indices[index++] = nextRing + tube;
                indices[index++] = nextRing + nextTube;
                indices[index++] = ring * tubeSegments + nextTube;
            }
        }

        var mesh = new Mesh { name = "Anneau de vitesse", vertices = vertices, normals = normals, triangles = indices };
        mesh.UploadMeshData(true);
        return mesh;
    }
}
