using UnityEngine;

public class ConeSnapToFloor : MonoBehaviour
{
    public Transform floorAnchor;          // 任意（無くてもOK）
    public string floorAnchorName = "FloorAnchor";

    public float yOffset = 0.02f;
    public bool lockToFloorY = true;

    float _fixedFloorY;
    bool _initialized;

    void Start()
    {
        TryInitFloorY();
    }

    void TryInitFloorY()
    {
        if (floorAnchor == null)
        {
            var go = GameObject.Find(floorAnchorName);
            if (go != null) floorAnchor = go.transform;
        }

        // floorAnchorが見つかったら、その時点のYを固定値として保存
        if (floorAnchor != null)
        {
            _fixedFloorY = floorAnchor.position.y;
            _initialized = true;
            Debug.Log($"[ConeSnapToFloor] fixed floorY={_fixedFloorY}");
        }
    }

    void LateUpdate()
    {
        if (!lockToFloorY) return;

        if (!_initialized)
        {
            TryInitFloorY();
            if (!_initialized) return;
        }

        var p = transform.position;
        p.y = _fixedFloorY + yOffset;
        transform.position = p;
    }
}
