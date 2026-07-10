using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RoomScanManager : MonoBehaviour
{
    [Header("Input (temporary)")]
    public KeyCode toggleScanKey = KeyCode.O;
    public KeyCode addPointKey = KeyCode.Space;
    public KeyCode undoKey = KeyCode.Backspace;
    public KeyCode clearKey = KeyCode.C;
    public KeyCode confirmKey = KeyCode.Return;

    [Header("Ray")]
    public Camera fallbackCamera;                 // Editor用（HMDカメラでもOK）
    public Transform rightHandRayOrigin;          // 右手Rayの出る位置（RightHandAnchor等）
    public float rayDistance = 10f;
    public LayerMask hitMask = ~0;                // 当てたい対象（床/壁/空間メッシュ等）

    [Header("Point marker")]
    public float markerRadius = 0.03f;
    public Color markerColor = new Color(0.2f, 0.6f, 1f, 1f); // 青
    public Transform markerParent;                // まとめ先（空なら自動生成）

    [Header("Build")]
    public RoomBuilder roomBuilder;               // 決定後に呼ぶ

    [Header("Ray Visual")]
    public LineRenderer line;
    public float rayWidth = 0.005f;             //先をみえるように

    [Header("Boundary Guide")]
    public RoomBoundaryGuide boundaryGuide;
    public LayerMask boundaryHitMask = ~0; // Boundaryだけにしたいなら後述

    [Header("Boundary")]
    public GuardianBoundaryColliderBuilder boundaryBuilder;




    public bool IsScanning { get; private set; }

    private readonly List<Vector3> _points = new();
    private readonly List<GameObject> _markers = new();

    private void Awake()
    {
        Debug.Log("[RoomScan] Awake");

        if (markerParent == null)
        {
            var go = new GameObject("ScanMarkers");
            markerParent = go.transform;
        }

        if (line == null)
        {
            var go = new GameObject("ScanRay");
            line = go.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Unlit/Color"));
            line.material.color = Color.cyan;
            line.positionCount = 2;
            line.startWidth = rayWidth;
            line.endWidth = rayWidth;
            line.useWorldSpace = true;
        }

        var rig = FindObjectOfType<OVRCameraRig>();

        // ★右手RayOriginを自動補完
        if (rightHandRayOrigin == null && rig != null && rig.rightHandAnchor != null)
            rightHandRayOrigin = rig.rightHandAnchor;

        // ★fallbackCameraを自動補完（CenterEyeのCamera）
        if (fallbackCamera == null && rig != null && rig.centerEyeAnchor != null)
            fallbackCamera = rig.centerEyeAnchor.GetComponent<Camera>();

        if (fallbackCamera == null) fallbackCamera = Camera.main;

        Debug.Log($"[RoomScan] rightHandRayOrigin={(rightHandRayOrigin ? rightHandRayOrigin.name : "NULL")}, fallbackCamera={(fallbackCamera ? fallbackCamera.name : "NULL")}");
    }


    private bool _hasHit;
    private Vector3 _lastHitPoint;

    private void Update()
    {
        // ★Rayは常に更新して表示（スキャンOFFでも追従）
        UpdateRayVisual();

        // ★AボタンでスキャンON/OFF（右手）
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            IsScanning = !IsScanning;
            Debug.Log($"[RoomScan] Scanning = {IsScanning}");

            if (boundaryGuide != null)
            {
                boundaryGuide.SetVisible(IsScanning);
                if (IsScanning) boundaryGuide.Rebuild(); // 最新のSceneから境界を更新
            }
            if (IsScanning && boundaryBuilder != null)
            {
                boundaryBuilder.Build();
                Debug.Log("[RoomScan] Boundary Build()");
            }
        }

        if (!IsScanning) return;

        // ★右トリガーで点追加（ヒットしてたら追加）
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            if (_hasHit)
            {
                Vector3 p = _lastHitPoint;

                if (boundaryGuide != null && boundaryGuide.TrySnap(p, out var snapped))
                    p = snapped;

                AddPoint(p);
            }
            else Debug.Log("[RoomScan] Ray hit nothing");
        }


        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)) UndoPoint();
        if (OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.RTouch)) ClearPoints();

        // ConfirmはStart（右）に
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.RTouch)) Confirm();
    }



    private void UpdateRayVisual()
    {
        if (line == null) return;

        Transform originT = rightHandRayOrigin;
        if (originT == null)
        {
            // 実行中にRigが生成されるケース対策：毎フレーム拾い直し
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null && rig.rightHandAnchor != null) originT = rightHandRayOrigin = rig.rightHandAnchor;
            if (fallbackCamera == null && rig != null && rig.centerEyeAnchor != null) fallbackCamera = rig.centerEyeAnchor.GetComponent<Camera>();
            if (fallbackCamera == null) fallbackCamera = Camera.main;
        }

        Ray ray;

        if (originT != null)
        {
            ray = new Ray(originT.position, originT.forward);
        }
        else if (fallbackCamera != null)
        {
            ray = new Ray(fallbackCamera.transform.position, fallbackCamera.transform.forward);
        }
        else
        {
            // どっちも無いなら描画できない
            line.enabled = false;
            return;
        }

        Vector3 rayEnd = ray.origin + ray.direction * rayDistance;

        _hasHit = Physics.Raycast(ray, out RaycastHit hit, rayDistance, hitMask);
        _lastHitPoint = _hasHit ? hit.point : rayEnd;

        line.enabled = true;
        line.SetPosition(0, ray.origin);
        line.SetPosition(1, _lastHitPoint);
    }


   

    private void AddPoint(Vector3 p)
    {
        _points.Add(p);

        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = $"VertexMarker_{_points.Count - 1}";
        marker.transform.SetParent(markerParent, worldPositionStays: true);
        marker.transform.position = p;
        marker.transform.localScale = Vector3.one * markerRadius * 2f;

        // 当たり判定は不要
        var col = marker.GetComponent<Collider>();
        if (col) Destroy(col);

        var r = marker.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Unlit/Color"));
            r.material.color = markerColor;
        }

        _markers.Add(marker);

        Debug.Log($"[RoomScan] Add point #{_points.Count}: {p}");
    }

    private void UndoPoint()
    {
        if (_points.Count == 0) return;

        int last = _points.Count - 1;
        _points.RemoveAt(last);

        if (_markers.Count > last && _markers[last] != null)
            Destroy(_markers[last]);
        if (_markers.Count > last)
            _markers.RemoveAt(last);

        Debug.Log("[RoomScan] Undo");
    }

    private void ClearPoints()
    {
        _points.Clear();
        foreach (var m in _markers) if (m) Destroy(m);
        _markers.Clear();
        Debug.Log("[RoomScan] Clear");
    }

    private void Confirm()
    {
        if (_points.Count < 4)
        {
            Debug.LogWarning("[RoomScan] Need at least 4 points to build a room.");
            return;
        }

        IsScanning = false;

        if (roomBuilder == null)
        {
            Debug.LogError("[RoomScan] RoomBuilder is not assigned.");
            return;
        }

        roomBuilder.BuildFromPoints(_points);
        Debug.Log($"[RoomScan] Confirm -> Build room with {_points.Count} points");
    }
}
