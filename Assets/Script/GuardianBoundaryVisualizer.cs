using UnityEngine;

public class GuardianBoundaryVisualizer : MonoBehaviour
{
    public OVRBoundary.BoundaryType boundaryType = OVRBoundary.BoundaryType.OuterBoundary;
    public LineRenderer line;
    public float yOffset = 0.02f;

    OVRCameraRig rig;

    void Awake()
    {
        rig = FindObjectOfType<OVRCameraRig>();

        if (line == null)
        {
            var go = new GameObject("GuardianLine");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = 0.01f;
            line.positionCount = 0;
            line.material = new Material(Shader.Find("Unlit/Color"));
            line.material.color = Color.white;
        }
    }

    void OnEnable()
    {
        DrawOnce();
    }

    [ContextMenu("DrawOnce")]
    public void DrawOnce()
    {
        var b = OVRManager.boundary;

        if (b == null || !b.GetConfigured())
        {
            Debug.LogWarning("[Guardian] Boundary not configured (Guardian無効/未設定の可能性)");
            line.positionCount = 0;
            return;
        }

        // 取得点は「TrackingSpace座標」なので trackingSpace でワールドへ変換する :contentReference[oaicite:1]{index=1}
        var pts = b.GetGeometry(boundaryType);
        if (pts == null || pts.Length < 3)
        {
            Debug.LogWarning("[Guardian] GetGeometry returned empty");
            line.positionCount = 0;
            return;
        }

        Transform ts = (rig != null && rig.trackingSpace != null) ? rig.trackingSpace : transform;

        line.positionCount = pts.Length + 1;
        for (int i = 0; i < pts.Length; i++)
        {
            var w = ts.TransformPoint(pts[i]);
            w.y += yOffset;
            line.SetPosition(i, w);
        }
        // 閉じる
        line.SetPosition(pts.Length, line.GetPosition(0));

        Debug.Log($"[Guardian] Draw {boundaryType} points={pts.Length}");
    }
}
