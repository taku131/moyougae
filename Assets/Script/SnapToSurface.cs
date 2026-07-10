using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;

public class SnapToSurface : MonoBehaviour
{
    [Header("Raycast Masks")]
    public LayerMask floorMask;             // Floorだけ
    public LayerMask wallMask;              // Wallだけ
    public float maxFloorDistance = 5.0f;
    public float maxWallDistance = 3.0f;

    [Header("Snapping")]
    public float surfaceOffset = 0.005f;
    public bool snapToFloor = true;
    public bool snapToWall = true;
    public bool preferFloor = false;        // ★壁が欲しいなら false 推奨
    public float floorBias = 0.15f;         // preferFloor=true の時だけ床を少し有利に

    [Header("Rotation")]
    public float yawStep = 90f;
    public bool zeroPitchRoll = true;

    [Header("Timing")]
    public float snapCooldown = 0.1f;

    private GrabInteractable _grab;
    private HandGrabInteractable _handGrab;

    private bool _wasSelected;
    private float _nextSnapTime;
    private float _yawAtRelease;
    private Quaternion _rotAtRelease;

    [SerializeField] private float freezeRotationTime = 0.2f;
    private Rigidbody _rb;

    private bool _pendingFloorRotation;
    private float _pendingYaw;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grab = GetComponent<GrabInteractable>();
        _handGrab = GetComponent<HandGrabInteractable>();

        // 未設定なら自動でレイヤー名から作る
        if (floorMask.value == 0) floorMask = LayerMask.GetMask("Floor");
        if (wallMask.value == 0) wallMask = LayerMask.GetMask("Wall");
    }

    private void Update()
    {
        bool selectedNow = IsSelectedNow();

        if (_wasSelected && !selectedNow)
        {
            _rotAtRelease = transform.rotation;         // ★追加
            _yawAtRelease = transform.eulerAngles.y;

            if (Time.time >= _nextSnapTime)
            {
                _nextSnapTime = Time.time + snapCooldown;
                DoSnap();
            }
        }

        _wasSelected = selectedNow;
    }
    private void LateUpdate()
    {
        if (!_pendingFloorRotation) return;

        // 掴み/物理の最終更新が終わったあとで回転を確定
        transform.rotation = Quaternion.Euler(0f, _pendingYaw, 0f);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[SnapToFloor] apply yaw in LateUpdate = {_pendingYaw:F1}°");

        _pendingFloorRotation = false;
    }


    private IEnumerator FreezeRotationBriefly()
    {
        if (_rb == null) yield break;

        var old = _rb.constraints;
        _rb.constraints = old | RigidbodyConstraints.FreezeRotation;

        // 角速度も止める（重要）
        _rb.angularVelocity = Vector3.zero;

        yield return new WaitForSeconds(freezeRotationTime);

        _rb.constraints = old;
    }

    private bool IsSelectedNow()
    {
        bool grabSelected = _grab != null && _grab.State == InteractableState.Select;
        bool handSelected = _handGrab != null && _handGrab.State == InteractableState.Select;
        return grabSelected || handSelected;
    }

    [ContextMenu("DoSnap")]
    public void DoSnap()
    {
        if (!TryGetWorldBounds(out var b)) return;

        bool hitFloor = false, hitWall = false;
        RaycastHit floorHit = default, wallHit = default;

        // 1) 床：真下（Floorレイヤーだけ）
        if (snapToFloor)
        {
            Vector3 origin = new Vector3(b.center.x, b.max.y + 0.05f, b.center.z);
            hitFloor = Physics.Raycast(origin, Vector3.down, out floorHit, maxFloorDistance, floorMask, QueryTriggerInteraction.Ignore);
        }

        // 2) 壁：世界4方向（Wallレイヤーだけ）
        if (snapToWall)
        {
            Vector3 origin = b.center + Vector3.up * 0.2f; // 少し上から撃つと床誤爆しにくい
            hitWall = TryRay4_World(origin, out wallHit);
        }

        if (!hitFloor && !hitWall) return;

        // どっちに吸着するか決める
        float floorD = hitFloor ? floorHit.distance : float.PositiveInfinity;
        float wallD = hitWall ? wallHit.distance : float.PositiveInfinity;

        if (preferFloor) floorD -= floorBias;

        if (wallD < floorD)
            SnapToWall(wallHit, b);
        else
            SnapToFloor(floorHit, b);
    }

    private bool TryRay4_World(Vector3 origin, out RaycastHit bestHit)
    {
        bestHit = default;
        bool any = false;
        float best = float.PositiveInfinity;

        Vector3[] dirs =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left
        };

        foreach (var d in dirs)
        {
            if (Physics.Raycast(origin, d, out RaycastHit hit, maxWallDistance, wallMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < best)
                {
                    best = hit.distance;
                    bestHit = hit;
                    any = true;
                }
            }
        }
        return any;
    }

    private void SnapToFloor(RaycastHit hit, Bounds b)
    {
        // ===== デバッグ（任意）=====
        float releaseYaw = _rotAtRelease.eulerAngles.y;
        float beforeYaw = transform.eulerAngles.y;
        Debug.Log($"[SnapToFloor] keep-rotation releaseYaw={releaseYaw:F1} beforeYaw={beforeYaw:F1}");

        // ===== Yだけ接地 =====
        // b.min.y は「今の回転/スケール込み」で計算されたワールド底面
        float bottomY = b.min.y;

        // 床に少し浮かせたいなら surfaceOffset を足す
        float targetBottomY = hit.point.y + surfaceOffset;

        // 差分だけYを上げ下げ
        float deltaY = targetBottomY - bottomY;
        transform.position += Vector3.up * deltaY;

        // ★回転は触らない
        Debug.Log($"[SnapToFloor] deltaY={deltaY:F4} afterPosY={transform.position.y:F3}");
    }




    private void SnapToWall(RaycastHit hit, Bounds b)
    {
        Vector3 n = hit.normal;

        float r =
            Mathf.Abs(n.x) * b.extents.x +
            Mathf.Abs(n.y) * b.extents.y +
            Mathf.Abs(n.z) * b.extents.z;

        Vector3 pivotToCenter = b.center - transform.position;
        Vector3 newCenter = hit.point + n * (r + surfaceOffset);
        transform.position = newCenter - pivotToCenter;

        // 壁基準で 0/90/180/270（壁に向く向きを基準にする）
        ApplyQuantizedYaw_OnWall(n);
        ApplyYaw_Wall(hit.normal);
        StartCoroutine(FreezeRotationBriefly());
        _rotAtRelease = transform.rotation;



    }

    private void ApplyQuantizedYaw_World()
    {
        float y = QuantizeAngle(_yawAtRelease, yawStep);

        if (zeroPitchRoll)
            transform.rotation = Quaternion.Euler(0f, y, 0f);
        else
        {
            var e = transform.eulerAngles;
            e.y = y;
            transform.rotation = Quaternion.Euler(e);
        }
    }

    private void ApplyQuantizedYaw_OnWall(Vector3 wallNormal)
    {
        Vector3 baseForward = Vector3.ProjectOnPlane(-wallNormal, Vector3.up).normalized;
        if (baseForward.sqrMagnitude < 0.0001f) baseForward = transform.forward;

        Quaternion baseRot = Quaternion.LookRotation(baseForward, Vector3.up);
        float baseYaw = baseRot.eulerAngles.y;

        float relative = Mathf.DeltaAngle(baseYaw, _yawAtRelease);
        float relativeSnapped = QuantizeAngle(relative, yawStep);

        float finalYaw = baseYaw + relativeSnapped;

        if (zeroPitchRoll)
            transform.rotation = Quaternion.Euler(0f, finalYaw, 0f);
        else
        {
            var e = transform.eulerAngles;
            e.y = finalYaw;
            transform.rotation = Quaternion.Euler(e);
        }
    }

    private static float QuantizeAngle(float angleDeg, float step)
    {
        if (step <= 0.0001f) return angleDeg;
        return Mathf.Round(angleDeg / step) * step;
    }

    private bool TryGetWorldBounds(out Bounds b)
    {
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { b = default; return false; }

        b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return true;
    }
    private void ApplyYaw_Floor()
    {
        // 離した瞬間の forward を水平面に落とす
        Vector3 f = Vector3.ProjectOnPlane(_rotAtRelease * Vector3.forward, Vector3.up);
        if (f.sqrMagnitude < 0.0001f) f = Vector3.forward;
        f.Normalize();

        // 世界Yaw（-180..180）を作る
        float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

        // 0/90/180/270に丸め
        float snapped = QuantizeAngle(yaw, yawStep);

        transform.rotation = Quaternion.Euler(0f, snapped, 0f);
    }


    private void ApplyYaw_Wall(Vector3 wallNormal)
    {
        // 壁に向く方向（水平面に落とす）
        Vector3 baseForward = Vector3.ProjectOnPlane(-wallNormal, Vector3.up).normalized;
        if (baseForward.sqrMagnitude < 0.0001f) baseForward = Vector3.forward;

        Quaternion baseRot = Quaternion.LookRotation(baseForward, Vector3.up);
        float baseYaw = baseRot.eulerAngles.y;

        // 離した瞬間のYawとの差分（相対角）
        float releaseYaw = _rotAtRelease.eulerAngles.y;
        float rel = Mathf.DeltaAngle(baseYaw, releaseYaw);

        // 相対角を 0/90/180/270 に丸める
        float relSnap = QuantizeAngle(rel, yawStep);

        // 壁基準 + 相対スナップ
        float finalYaw = baseYaw + relSnap;

        transform.rotation = Quaternion.Euler(0f, finalYaw, 0f); // zeroPitchRoll前提
    }

}
