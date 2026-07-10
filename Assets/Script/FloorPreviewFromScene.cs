using UnityEngine;

public class FloorPreviewFromScene : MonoBehaviour
{
    public RoomBoundaryGuide boundaryGuide;
    public Material floorMat;
    public float yOffset = 0.01f;     // Z-fighting防止
    public float thickness = 0.01f;

    GameObject _floor;
    public Transform FloorAnchorTransform { get; private set; }


    void Awake()
    {
        if (boundaryGuide == null) boundaryGuide = FindObjectOfType<RoomBoundaryGuide>();
    }

    void Start()
    {
        Invoke(nameof(BuildFloor), 2.9f);
    }

    void BuildFloor()
    {
        if (boundaryGuide == null || boundaryGuide.floorPlane == null)
        {
            Debug.LogWarning("[FloorPreview] floorPlane not ready.");
            return;
        }

        var plane = boundaryGuide.floorPlane;
        Vector2 size = plane.Dimensions; // ローカルXYの幅高さ
        float w = size.x;
        float d = size.y;

        _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _floor.name = "FloorPreview";
        _floor.transform.SetParent(transform, false);

        // plane.transform は床の向き・位置を持つので、それに合わせる
        _floor.transform.position = plane.transform.position + plane.transform.up * (yOffset + thickness * 0.5f);
        _floor.transform.rotation = plane.transform.rotation;

        // ローカルXYを床の幅奥行として扱ってるので、CubeのX=幅、Z=奥行に合わせる
        _floor.transform.localScale = new Vector3(w, thickness, d);

        // 当たり判定不要ならCollider消す
        Destroy(_floor.GetComponent<Collider>());

        var r = _floor.GetComponent<Renderer>();
        if (r != null && floorMat != null) r.material = floorMat;

        Debug.Log("[FloorPreview] Built floor preview.");
    }
}
