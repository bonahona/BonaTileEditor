using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MapSegmentPreview : MonoBehaviour
{
    public MapSegment MapSegment;
    public MeshFilter MeshFilter;

    public MapSegmentLayer CurrentLayer;
    public MapSegmentPaletteSelection CurrentSelection;

    void Start ()
    {
        #if UNITY_EDITOR
        #else
        GameObject.Destroy(gameObject);
        #endif
    }

    public void Init()
    {
        MeshFilter = GetComponent<MeshFilter>();
    }

    public void SetPreviewZone(Point point)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int index = 0;

        var meshFilter = gameObject.GetComponent<MeshFilter>();
        var mesh = new Mesh();

        AddVertices(point, 0, 0, vertices, MapSegment.GridTileSize);
        AddUvs(null, 0, uvs);
        index = AddTris(index, tris);
        AddNormals(normals);

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = null;
        meshFilter.mesh = mesh;
    }

    protected void AddVertices(Point offset, int x, int y, List<Vector3> vertices, Vector2 tileSize)
    {
        float sizeX = tileSize.x;
        float sizeY = tileSize.y;

        vertices.Add(new Vector3(x * sizeX, y * sizeY, 0));
        vertices.Add(new Vector3((x + 1) * sizeX, y * sizeY, 0));
        vertices.Add(new Vector3(x * sizeX, (y + 1) * sizeY, 0));
        vertices.Add(new Vector3((x + 1) * sizeX, (y + 1) * sizeY, 0));
    }

    protected void AddUvs(TileSetLayer tileSetLayer, int tile, List<Vector2> uvs)
    {
        for (int i = 0; i < 4; i++) {
            uvs.Add(Vector2.zero);
        }
        return;

        //foreach (var uvCords in tileSetLayer.Tiles[tile].UvCords) {
        //    uvs.Add(uvCords);
        //}
    }

    protected int AddTris(int startIndex, List<int> tris)
    {
        tris.Add(startIndex + 1);
        tris.Add(startIndex + 2);
        tris.Add(startIndex + 3);

        tris.Add(startIndex + 2);
        tris.Add(startIndex + 1);
        tris.Add(startIndex);
        return startIndex + 4;
    }

    protected void AddNormals(List<Vector3> normals)
    {
        for (int i = 0; i < 4; i++) {
            normals.Add(Vector3.forward);
        }
    }
}
