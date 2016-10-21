using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MapSegmentPreview : MonoBehaviour
{
    public MapSegment MapSegment;
    public MeshFilter MeshFilter;

    public MapSegmentPaletteSelection CurrentSelection;

    void Start ()
    {
        // This object should only be alive in the editor. If it does not it will ve created automatically by the editor script so
        // removing it is never a problem.
        GameObject.Destroy(gameObject);
    }

    public void Init()
    {
        MeshFilter = GetComponent<MeshFilter>();
    }

    public void SetPreviewZoneSingle(MapSegmentPaletteSelection selection, Point startPoint)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int index = 0;

        var scaledOffset = GetScaledOffset(startPoint, MapSegment.GridTileSize);
        for (int y = 0; y < selection.Height; y++) {
            for (int x = 0; x < selection.Width; x++) {
                if (MapSegment.ValidateBounds(x, y, startPoint)) {

                    // Adjust the y value as its draw in the opposite direction (Start in the top left corner)
                    var adjustedY = (selection.Height - y) - 1;
                    AddVertices(scaledOffset, x, -y, vertices, MapSegment.GridTileSize);
                    AddUvs(selection.GetTileType(x, adjustedY), uvs, MapSegment.CurrentLayer.TileSetLayer);
                    index = AddTris(index, tris);
                    AddNormals(normals);
                }
            }
        }

        UpdateMesh(vertices, uvs, tris, normals);
    }

    public void SetPreviewZoneBlock(MapSegmentPaletteSelection selection, Point startPoint, Point endPoint)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int index = 0;

        var start = new Point(Mathf.Min(startPoint.X, endPoint.X), Mathf.Min(startPoint.Y, endPoint.Y));
        var end = new Point(Mathf.Max(startPoint.X, endPoint.X), Mathf.Max(startPoint.Y, endPoint.Y));

        // Some of the points are not on the mapsegment and this will look really weird if its allowed
        if (!MapSegment.ValidateBounds(start) || !MapSegment.ValidateBounds(end)) {
            return;
        }

        Debug.Log(string.Format("{0}; {1}", start, end));
        var scaledOffset = Vector3.zero;
        for (int y = start.Y; y <= end.Y; y ++) {
            for (int x = start.X; x <= end.X; x ++) {
                AddVertices(scaledOffset, x, y, vertices, MapSegment.GridTileSize);
                AddUvs(selection.GetSingleSelecttion(), uvs, MapSegment.CurrentLayer.TileSetLayer);
                index = AddTris(index, tris);
                AddNormals(normals);
            }
        }

        UpdateMesh(vertices, uvs, tris, normals);
    }

    protected void UpdateMesh(List<Vector3> vertices, List<Vector2> uvs, List<int> tris, List<Vector3> normals)
    {
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        var meshRenderer = gameObject.GetComponent<MeshRenderer>();
        var mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.normals = normals.ToArray();


        if (meshRenderer.sharedMaterial == null) {
            meshFilter.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        meshRenderer.sharedMaterial.mainTexture = MapSegment.CurrentLayer.TileSetLayer.Texture;

        meshFilter.mesh = null;
        meshFilter.mesh = mesh;
    }

    protected void AddVertices(Vector3 offset, int x, int y, List<Vector3> vertices, Vector2 tileSize)
    {
        float sizeX = tileSize.x;
        float sizeY = tileSize.y;

        vertices.Add(new Vector3(x * sizeX, y * sizeY, 0) + offset);
        vertices.Add(new Vector3((x + 1) * sizeX, y * sizeY, 0) + offset);
        vertices.Add(new Vector3(x * sizeX, (y + 1) * sizeY, 0) + offset);
        vertices.Add(new Vector3((x + 1) * sizeX, (y + 1) * sizeY, 0) + offset);
    }

    protected Vector3 GetScaledOffset(Point offset, Vector2 tileSize)
    {
        var result = new Vector3();

        result.x = offset.X * tileSize.x;
        result.y = offset.Y * tileSize.y;

        return result;
    }

    protected void AddUvs(int tile, List<Vector2> uvs, TileSetLayer layer)
    {
        foreach(var uvCord in layer.Tiles[tile].UvCords) {
            uvs.Add(uvCord);
        }
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
