using UnityEngine;

public class GuardianBoundaryColliderBuilder : MonoBehaviour
{
    public string boundaryLayerName = "Boundary";
    public OVRBoundary.BoundaryType boundaryType = OVRBoundary.BoundaryType.PlayArea;

    public float wallHeight = 2.0f;
    public float thickness = 0.05f;

    Transform root;
    OVRCameraRig rig;

    void Awake()
    {
        rig = FindObjectOfType<OVRCameraRig>();
    }

    [ContextMenu("Build")]
    public void Build()
    {
        Clear();
        Debug.Log($"[Guardian] boundaryType={boundaryType}");


        var b = OVRManager.boundary;
        Debug.Log($"[Guardian] boundaryNull={(b == null)} configured={(b != null && b.GetConfigured())}");

        if (b == null || !b.GetConfigured())
        {
            Debug.LogWarning("[Guardian] NOT configured. Quest本体で境界線(Guardian)が有効か確認して");
            return;
        }

        var pts = b.GetGeometry(boundaryType);
        if (pts == null || pts.Length < 3)
        {
            Debug.LogWarning($"[Guardian] {boundaryType} empty -> fallback to PlayArea");
            boundaryType = OVRBoundary.BoundaryType.PlayArea;
            pts = b.GetGeometry(boundaryType);
        }

        Debug.Log($"[Guardian] geometry points = {(pts == null ? -1 : pts.Length)} type={boundaryType}");

        if (pts == null || pts.Length < 3)
        {
            Debug.LogWarning("[Guardian] geometry is empty");
            return;
        }

        Transform ts = (rig != null && rig.trackingSpace != null) ? rig.trackingSpace : transform;

        root = new GameObject("GuardianBoundaryColliders").transform;
        root.SetParent(ts, false);


        int layer = LayerMask.NameToLayer(boundaryLayerName);

        for (int i = 0; i < pts.Length; i++)
        {
            int j = (i + 1) % pts.Length;

            Vector3 a = ts.TransformPoint(pts[i]);
            Vector3 c = ts.TransformPoint(pts[j]);

            Vector3 mid = (a + c) * 0.5f;
            Vector3 dir = (c - a);
            float len = dir.magnitude;
            if (len < 0.01f) continue;

            var go = new GameObject($"BEdge_{i}");
            go.transform.SetParent(root, false);
            go.transform.position = mid + Vector3.up * (wallHeight * 0.5f);
            go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            if (layer >= 0) go.layer = layer;

            var box = go.AddComponent<BoxCollider>();
            // Zを厚み、Xを長さ、Yを高さ
            box.size = new Vector3(len, wallHeight, thickness);
            box.center = Vector3.zero;
        }

        Debug.Log("[GuardianCol] Built colliders");
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        if (root != null) Destroy(root.gameObject);
        root = null;
    }
}
