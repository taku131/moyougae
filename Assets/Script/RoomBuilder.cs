using System.Collections.Generic;
using UnityEngine;

public class RoomBuilder : MonoBehaviour
{
    [Header("Layers")]
    public string floorLayerName = "Floor";
    public string wallLayerName = "Wall";

    [Header("Room size")]
    public float defaultCeilingHeight = 3.0f; // 点群にYが少ない場合の天井高さ
    public float thickness = 0.05f;           // 壁/床の厚み
    public float padding = 0.02f;             // 少しだけ余裕

    [Header("Visual (optional)")]
    public bool createVisuals = true;
    public Material floorMaterial;
    public Material wallMaterial;


    public Vector3 LastMin { get; private set; }
    public Vector3 LastMax { get; private set; }
    public float LastFloorY { get; private set; }
    public float LastCeilingY { get; private set; }


    [Header("Root")]
    public Transform roomRoot;                // 生成先。空なら自動生成

    public void ClearRoom()
    {
        if (roomRoot != null) Destroy(roomRoot.gameObject);
        roomRoot = null;
    }

    public void BuildFromPoints(List<Vector3> points)
    {
        // 既存削除
        ClearRoom();

        // AABB
        Vector3 min = points[0];
        Vector3 max = points[0];
        for (int i = 1; i < points.Count; i++)
        {
            min = Vector3.Min(min, points[i]);
            max = Vector3.Max(max, points[i]);
        }

        // 床は水平想定：床Yは最小Y
        float floorY = min.y;

        // 天井Yは「点群のmaxY」が十分高いならそれを使う、低いならデフォルト
        float ceilingY = (max.y - min.y) > 1.0f ? max.y : (floorY + defaultCeilingHeight);

        // XZは少しだけ余裕
        min.x -= padding; min.z -= padding;
        max.x += padding; max.z += padding;

        var rootGO = new GameObject("RoomRoot");
        roomRoot = rootGO.transform;

        // 面作成
        CreateFloor(min, max, floorY);
        CreateWalls(min, max, floorY, ceilingY);

        LastMin = min;
        LastMax = max;
        LastFloorY = floorY;
        LastCeilingY = ceilingY;


        Debug.Log($"[RoomBuilder] Room AABB X[{min.x},{max.x}] Z[{min.z},{max.z}] Y[{floorY},{ceilingY}]");
    }

    private void CreateFloor(Vector3 min, Vector3 max, float floorY)
    {
        var go = new GameObject("Floor");
        go.transform.SetParent(roomRoot, false);

        int layer = LayerMask.NameToLayer(floorLayerName);
        if (layer >= 0) go.layer = layer;

        float width = (max.x - min.x);
        float depth = (max.z - min.z);

        // Collider
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;

        col.size = new Vector3(width, thickness, depth);
        col.center = new Vector3((min.x + max.x) * 0.5f, floorY - thickness * 0.5f, (min.z + max.z) * 0.5f);

        // Visual
        if (createVisuals)
        {
            var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.name = "FloorVisual";
            vis.transform.SetParent(go.transform, false);
            vis.layer = go.layer;


            vis.transform.position = new Vector3((min.x + max.x) * 0.5f, floorY - thickness * 0.5f, (min.z + max.z) * 0.5f);
            vis.transform.localScale = new Vector3(width, thickness, depth);

            // PrimitiveのColliderは不要（2重になる）
            Destroy(vis.GetComponent<Collider>());

            var r = vis.GetComponent<Renderer>();
            if (r != null && floorMaterial != null) r.material = floorMaterial;
        }
    }


    private void CreateWalls(Vector3 min, Vector3 max, float floorY, float ceilingY)
    {
        float height = ceilingY - floorY;
        float cx = (min.x + max.x) * 0.5f;
        float cz = (min.z + max.z) * 0.5f;

        // +Z wall
        CreateWall("Wall_North(+Z)",
            new Vector3(max.x - min.x, height, thickness),
            new Vector3(cx, floorY + height * 0.5f, max.z + thickness * 0.5f));

        // -Z wall
        CreateWall("Wall_South(-Z)",
            new Vector3(max.x - min.x, height, thickness),
            new Vector3(cx, floorY + height * 0.5f, min.z - thickness * 0.5f));

        // +X wall
        CreateWall("Wall_East(+X)",
            new Vector3(thickness, height, max.z - min.z),
            new Vector3(max.x + thickness * 0.5f, floorY + height * 0.5f, cz));

        // -X wall
        CreateWall("Wall_West(-X)",
            new Vector3(thickness, height, max.z - min.z),
            new Vector3(min.x - thickness * 0.5f, floorY + height * 0.5f, cz));
    }

    private void CreateWall(string name, Vector3 size, Vector3 center)
    {
        var go = new GameObject(name);
        go.transform.SetParent(roomRoot, false);

        int layer = LayerMask.NameToLayer(wallLayerName);
        if (layer >= 0) go.layer = layer;

        // Collider
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;

        col.size = size;
        col.center = center;

        // Visual
        if (createVisuals)
        {
            var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.name = "WallVisual";
            vis.transform.SetParent(go.transform, false);
            vis.layer = go.layer;


            vis.transform.position = center;
            vis.transform.localScale = size;

            Destroy(vis.GetComponent<Collider>());

            var r = vis.GetComponent<Renderer>();
            if (r != null && wallMaterial != null) r.material = wallMaterial;
        }
    }

}
