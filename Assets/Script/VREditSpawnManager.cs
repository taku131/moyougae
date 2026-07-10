using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VREditSpawnManager : MonoBehaviour
{
    public Transform head;
    public GameObject conePrefab;
    public float spawnDistance = 1.2f;
    public float spawnHeight = 0.9f;
    public float spacing = 0.18f;

    public BuildProgressUI progressUI;
    public RoomBuilder roomBuilder;

    [SerializeField] private GameObject doorPrefab;
    private GameObject _doorInstance;

    // 生成したものを管理
    readonly List<GameObject> _spawnedObjects = new();

    Transform[] _blue = new Transform[4];
    Transform _red;
    Transform _green;

    // ★状態管理
    bool conesSpawned = false;  // コーンは一度だけ
    bool finalized = false;     // 確定も一度だけ
    bool isBuilding = false;    // 生成中の連打防止

    void Awake()
    {
        if (head == null)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null) head = rig.centerEyeAnchor;
        }
        if (roomBuilder == null) roomBuilder = FindObjectOfType<RoomBuilder>();
        if (progressUI == null) progressUI = FindObjectOfType<BuildProgressUI>(true);
    }

    void Update()
    {
        // 生成中 or 確定後は入力を無効化（必要なら確定後に別ボタンでリセット可能）
        if (isBuilding || finalized) return;

        // A：コーン一式を出す（★一度だけ）
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            if (conesSpawned)
            {
                Debug.Log("[VREdit] Cones already spawned. (Aは1回だけ)");
                return;
            }

            SpawnAllCones();
            conesSpawned = true;
            Debug.Log("[VREdit] Cones spawned.");
        }

        // B：部屋生成（★コーンが出ているときだけ、1回だけ）
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            if (!conesSpawned)
            {
                Debug.LogWarning("[VREdit] 先にAでコーンを出してね");
                return;
            }

            StartCoroutine(BuildRoomRoutine());
        }
    }

    void SpawnAllCones()
    {
        if (conePrefab == null || head == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = head.forward;

        Vector3 basePos = head.position + forward * spawnDistance;
        basePos.y = head.position.y - 0.2f + spawnHeight;

        // 青4（四隅）
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = basePos + head.right * ((i - 1.5f) * spacing);
            _blue[i] = SpawnCone(p, Color.cyan, $"CornerBlue_{i}");
        }

        // 赤（ドア）
        Vector3 redPos = basePos + forward * 0.25f + head.right * (-2.0f * spacing);
        _red = SpawnCone(redPos, Color.red, "DoorRed");

        // ★緑（窓）1個だけ
        Vector3 greenPos = basePos + forward * 0.25f + head.right * (2.0f * spacing);
        _green = SpawnCone(greenPos, Color.green, "WindowGreen");
    }


    Transform SpawnCone(Vector3 pos, Color color, string name)
    {
        var go = Instantiate(conePrefab, pos, Quaternion.identity, transform);
        go.name = name;

        var r = go.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            var mat = new Material(r.material);
            mat.color = color;
            r.material = mat;
        }

        _spawnedObjects.Add(go);
        return go.transform;
    }

    void ClearSpawnedCones()
    {
        foreach (var go in _spawnedObjects)
            if (go != null) Destroy(go);

        _spawnedObjects.Clear();

        for (int i = 0; i < 4; i++) _blue[i] = null;
        _red = null;
        _green = null;
    }

    bool BuildRoomFromBlueCorners()
    {
        if (roomBuilder == null)
        {
            Debug.LogError("[VREdit] RoomBuilder not found.");
            return false;
        }

        for (int i = 0; i < 4; i++)
        {
            if (_blue[i] == null)
            {
                Debug.LogWarning("[VREdit] Blue corners are not ready.");
                return false;
            }
        }

        var pts = new List<Vector3>(4);
        for (int i = 0; i < 4; i++) pts.Add(_blue[i].position);

        roomBuilder.BuildFromPoints(pts);
        Debug.Log("[VREdit] Build room from blue corners.");
        return true;
    }

    [SerializeField] private float doorHeightOffset = 1.3f; // ★床から+1.5m
    void PlaceDoorFromRed()
    {
        if (_red == null)
        {
            Debug.LogWarning("[VREdit] Red cone is null. ドア位置が未設定");
            return;
        }
        if (doorPrefab == null)
        {
            Debug.LogWarning("[VREdit] doorPrefab is null. Inspectorで設定してね");
            return;
        }
        if (roomBuilder == null)
        {
            Debug.LogWarning("[VREdit] roomBuilder is null");
            return;
        }

        Vector3 min = roomBuilder.LastMin;
        Vector3 max = roomBuilder.LastMax;

        Vector3 p = _red.position;
        //p.y = roomBuilder.LastFloorY + doorHeightOffset;
        p.y = roomBuilder.LastFloorY;

        float dWest = Mathf.Abs(p.x - min.x);
        float dEast = Mathf.Abs(max.x - p.x);
        float dSouth = Mathf.Abs(p.z - min.z);
        float dNorth = Mathf.Abs(max.z - p.z);

        float best = dWest;
        int wall = 0; // 0=W,1=E,2=S,3=N
        if (dEast < best) { best = dEast; wall = 1; }
        if (dSouth < best) { best = dSouth; wall = 2; }
        if (dNorth < best) { best = dNorth; wall = 3; }

        Vector3 doorPos = p;
        Quaternion doorRot = Quaternion.identity;

        switch (wall)
        {
            case 0: doorPos.x = min.x; doorRot = Quaternion.LookRotation(Vector3.right, Vector3.up); break;
            case 1: doorPos.x = max.x; doorRot = Quaternion.LookRotation(Vector3.left, Vector3.up); break;
            case 2: doorPos.z = min.z; doorRot = Quaternion.LookRotation(Vector3.forward, Vector3.up); break;
            case 3: doorPos.z = max.z; doorRot = Quaternion.LookRotation(Vector3.back, Vector3.up); break;
        }

        if (_doorInstance != null) Destroy(_doorInstance);
        _doorInstance = Instantiate(doorPrefab, doorPos, doorRot);

        Debug.Log($"[VREdit] Door placed. wall={wall} pos={doorPos}");
    }

    [SerializeField] private GameObject windowPrefab;
    private GameObject _windowInstance;

    [SerializeField] private float windowHeightOffset = 1.5f; // ★床から+1.5m
    private void PlaceWindowFromGreen()
    {
        if (_green == null)
        {
            Debug.LogWarning("[VREdit] Green cone (_green) is null. 窓位置が未設定");
            return;
        }
        if (windowPrefab == null)
        {
            Debug.LogWarning("[VREdit] windowPrefab is null. Inspectorで設定してね");
            return;
        }
        if (roomBuilder == null)
        {
            Debug.LogWarning("[VREdit] roomBuilder is null");
            return;
        }

        // RoomBuilderに LastMin/LastMax/LastFloorY がある前提
        Vector3 min = roomBuilder.LastMin;
        Vector3 max = roomBuilder.LastMax;

        Vector3 p = _green.position;

        // ★高さは床+1.5mに固定
        p.y = roomBuilder.LastFloorY + windowHeightOffset;

        float dWest = Mathf.Abs(p.x - min.x);
        float dEast = Mathf.Abs(max.x - p.x);
        float dSouth = Mathf.Abs(p.z - min.z);
        float dNorth = Mathf.Abs(max.z - p.z);

        float best = dWest;
        int wall = 0; // 0=W,1=E,2=S,3=N
        if (dEast < best) { best = dEast; wall = 1; }
        if (dSouth < best) { best = dSouth; wall = 2; }
        if (dNorth < best) { best = dNorth; wall = 3; }

        Vector3 windowPos = p;
        Quaternion windowRot = Quaternion.identity;

        // ★ドアと同じく「部屋の内側を向く」回転
        switch (wall)
        {
            case 0: windowPos.x = min.x; windowRot = Quaternion.LookRotation(Vector3.right, Vector3.up); break; // West
            case 1: windowPos.x = max.x; windowRot = Quaternion.LookRotation(Vector3.left, Vector3.up); break; // East
            case 2: windowPos.z = min.z; windowRot = Quaternion.LookRotation(Vector3.forward, Vector3.up); break; // South
            case 3: windowPos.z = max.z; windowRot = Quaternion.LookRotation(Vector3.back, Vector3.up); break; // North
        }

        if (_windowInstance != null) Destroy(_windowInstance);
        _windowInstance = Instantiate(windowPrefab, windowPos, windowRot);

        Debug.Log($"[VREdit] Window placed. wall={wall} pos={windowPos} hOffset={windowHeightOffset}");
    }


    IEnumerator BuildRoomRoutine()
    {
        isBuilding = true;
        progressUI?.Begin("壁を生成中…");
        yield return null; // UIを確実に出す

        bool ok = false;
        try
        {
            ok = BuildRoomFromBlueCorners(); // ★ここで1回だけ
            if (ok)
            {
                PlaceDoorFromRed();
                PlaceWindowFromGreen();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            ok = false;
        }

        if (ok)
        {
            progressUI?.Finish("生成完了！");
            ClearSpawnedCones();   // ★確定したら消す
            finalized = true;      // ★以後A/Bを無効化
        }
        else
        {
            progressUI?.Fail("生成に失敗しました");
        }

        isBuilding = false;
    }
}
