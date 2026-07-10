using System.Collections;
using UnityEngine;

public class RoomBoundaryGuide : MonoBehaviour
{
    [Header("Scene")]
    public OVRSceneManager sceneManager;

    [Header("Pick floor")]
    public bool autoPickLargestFloor = true;
    public OVRScenePlane floorPlane; // 手動指定も可

    [Header("Visual")]
    public bool visible = false;
    public float lineWidth = 0.01f;

    [Header("Collider (boundary)")]
    public string boundaryLayerName = "Boundary";
    public float colliderThickness = 0.03f; // 境界線の太さ（狙いやすさ）
    public float colliderDepth = 0.08f;     // 高さ方向（床から少し浮かせても良い）

    [Header("Snap")]
    public bool snapToNearestCorner = true;
    public float cornerSnapRadius = 0.25f; // 角に吸い付く距離（m）

    LineRenderer _lr;
    Transform _root;
    Vector3[] _corners = new Vector3[4];
    int _boundaryLayer = -1;
    bool _built;

    void Awake()
    {
        _boundaryLayer = LayerMask.NameToLayer(boundaryLayerName);

        _root = new GameObject("BoundaryRoot").transform;
        _root.SetParent(transform, false);

        var go = new GameObject("BoundaryLine");
        go.transform.SetParent(_root, false);

        _lr = go.AddComponent<LineRenderer>();
        _lr.material = new Material(Shader.Find("Unlit/Color"));
        _lr.material.color = Color.cyan;
        _lr.positionCount = 5;
        _lr.startWidth = lineWidth;
        _lr.endWidth = lineWidth;
        _lr.useWorldSpace = true;
        _lr.enabled = false;
    }

    IEnumerator Start()
    {
        if (sceneManager == null) sceneManager = FindObjectOfType<OVRSceneManager>();

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 20; i++) // 最大10秒待つ
        {
            Rebuild();
            if (floorPlane != null)
            {
                Debug.Log("[BoundaryGuide] floorPlane acquired.");
                break;
            }
            Debug.Log("[BoundaryGuide] waiting for floorPlane...");
            yield return new WaitForSeconds(0.5f);
        }

        SetVisible(visible);
    }



    public void SetVisible(bool on)
    {
        visible = on;
        if (_lr != null) _lr.enabled = on && _built;

        // Colliderの有効/無効も同期
        if (_root != null)
        {
            for (int i = 0; i < _root.childCount; i++)
            {
                var c = _root.GetChild(i).GetComponent<Collider>();
                if (c != null) c.enabled = on && _built;
            }
        }
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        Clear();

        if (autoPickLargestFloor && floorPlane == null)
        {
            floorPlane = FindLargestFloorPlane();
        }

        if (floorPlane == null)
        {
            Debug.LogWarning("[BoundaryGuide] floorPlane not found.");
            _built = false;
            SetVisible(visible);
            return;
        }

        ComputeFloorRectCorners(floorPlane, _corners);

        // Line
        _lr.SetPosition(0, _corners[0]);
        _lr.SetPosition(1, _corners[1]);
        _lr.SetPosition(2, _corners[2]);
        _lr.SetPosition(3, _corners[3]);
        _lr.SetPosition(4, _corners[0]);

        // Edge colliders
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            CreateEdgeCollider($"Edge_{i}", _corners[i], _corners[j], floorPlane.transform.up);
        }

        _built = true;
        SetVisible(visible);

        Debug.Log("[BoundaryGuide] Built boundary from floor plane.");
    }

    public bool TrySnap(Vector3 hitPoint, out Vector3 snapped)
    {
        snapped = hitPoint;
        if (!_built) return false;

        if (snapToNearestCorner)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float d = Vector3.Distance(hitPoint, _corners[i]);
                if (d < bestD) { bestD = d; best = i; }
            }

            if (bestD <= cornerSnapRadius)
            {
                snapped = _corners[best];
                return true;
            }
        }

        // 角に吸わない場合はそのまま
        return false;
    }

    // ===== helpers =====

    void Clear()
    {
        if (_root == null) return;

        // 子（Edgeコライダー）を消す。ただし BoundaryLine は残す
        for (int i = _root.childCount - 1; i >= 0; i--)
        {
            var ch = _root.GetChild(i);
            if (ch.name.StartsWith("Edge_")) Destroy(ch.gameObject);
        }
        _built = false;
        if (_lr != null) _lr.enabled = false;
    }

    OVRScenePlane FindLargestFloorPlane()
    {
        var anchors = FindObjectsOfType<OVRSceneAnchor>(true);
        OVRScenePlane bestPlane = null;
        float bestArea = 0f;

        foreach (var a in anchors)
        {
            var plane = a.GetComponent<OVRScenePlane>();
            var sem = a.GetComponent<OVRSemanticClassification>();
            if (plane == null || sem == null) continue;
            if (!sem.Contains("floor")) continue;

            var d = plane.Dimensions;
            float area = d.x * d.y;
            if (area > bestArea)
            {
                bestArea = area;
                bestPlane = plane;
            }
        }
        return bestPlane;
    }

    static void ComputeFloorRectCorners(OVRScenePlane plane, Vector3[] outCorners4)
    {
        // plane.Dimensions はローカルXY方向の幅/高さとして扱う（あなたのBoxCollider生成と同じ）
        Vector2 size = plane.Dimensions;
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;

        // ローカル座標（XY平面）→ ワールドへ
        // 順序: 0:(-x,-y), 1:(-x,+y), 2:(+x,+y), 3:(+x,-y)
        outCorners4[0] = plane.transform.TransformPoint(new Vector3(-hx, -hy, 0f));
        outCorners4[1] = plane.transform.TransformPoint(new Vector3(-hx, +hy, 0f));
        outCorners4[2] = plane.transform.TransformPoint(new Vector3(+hx, +hy, 0f));
        outCorners4[3] = plane.transform.TransformPoint(new Vector3(+hx, -hy, 0f));
    }

    void CreateEdgeCollider(string name, Vector3 a, Vector3 b, Vector3 planeNormal)
    {
        var edgeGo = new GameObject(name);
        edgeGo.transform.SetParent(_root, true);

        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = (b - a);
        float len = dir.magnitude;
        if (len < 0.0001f) return;
        dir /= len;

        // local X をエッジ方向にしたい
        // up=planeNormal, forward=cross(up, dir) だと right が dir になる
        Vector3 forward = Vector3.Cross(planeNormal, dir).normalized;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

        edgeGo.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(forward, planeNormal));

        if (_boundaryLayer >= 0) edgeGo.layer = _boundaryLayer;

        var box = edgeGo.AddComponent<BoxCollider>();
        box.size = new Vector3(len, colliderDepth, colliderThickness);
        box.center = Vector3.zero;
    }
}
