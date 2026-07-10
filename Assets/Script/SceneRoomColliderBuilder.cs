using System.Collections;
using UnityEngine;

public class SceneRoomColliderBuilder : MonoBehaviour
{
    public OVRSceneManager sceneManager;

    [Header("Layers")]
    public string floorLayerName = "Floor";
    public string wallLayerName = "Wall";

    [Header("Collider")]
    public float thickness = 0.05f;

    private Transform _roomRoot;

    private IEnumerator Start()
    {
        if (sceneManager == null)
            sceneManager = FindObjectOfType<OVRSceneManager>();

        if (sceneManager == null)
        {
            Debug.LogError("[SceneRoom] OVRSceneManager not found");
            yield break;
        }

        // ★Scene APIがアンカーを生成するまで待つ（2.5秒固定はやめる）
        float timeout = 20f;
        float t = 0f;

        while (t < timeout)
        {
            var anchorsNow = FindObjectsOfType<OVRSceneAnchor>(true);
            if (anchorsNow != null && anchorsNow.Length > 0) break;

            t += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"[SceneRoom] wait done. anchors={FindObjectsOfType<OVRSceneAnchor>(true).Length}");

        Build();
    }


    [ContextMenu("Build")]
    public void Build()
    {
        Clear();

        _roomRoot = new GameObject("SceneRoomRoot").transform;
        _roomRoot.SetParent(transform, false);

        int floorLayer = LayerMask.NameToLayer(floorLayerName);
        int wallLayer = LayerMask.NameToLayer(wallLayerName);

        var anchors = FindObjectsOfType<OVRSceneAnchor>(true);
        Debug.Log($"[SceneRoom] Anchors: {anchors.Length}");

        int floorCount = 0;
        int wallCount = 0;

        foreach (var anchor in anchors)
        {
            var plane = anchor.GetComponent<OVRScenePlane>();
            var semantic = anchor.GetComponent<OVRSemanticClassification>();

            if (plane == null || semantic == null) continue;

            bool isFloor = semantic.Contains("floor");
            bool isWall = semantic.Contains("wall");

            if (!isFloor && !isWall) continue;

            GameObject go = new GameObject(
                $"Collider_{(isFloor ? "Floor" : "Wall")}"
            );

            go.transform.SetParent(_roomRoot, false);
            go.transform.position = anchor.transform.position;
            go.transform.rotation = anchor.transform.rotation;

            if (isFloor && floorLayer >= 0) go.layer = floorLayer;
            if (isWall && wallLayer >= 0) go.layer = wallLayer;

            var box = go.AddComponent<BoxCollider>();

            Vector2 size = plane.Dimensions;
            box.size = new Vector3(size.x, size.y, thickness);
            box.center = Vector3.zero;

            if (isFloor) floorCount++;
            if (isWall) wallCount++;
        }

        Debug.Log($"[SceneRoom] Built Floor={floorCount}, Wall={wallCount}");
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        if (_roomRoot != null)
        {
            Destroy(_roomRoot.gameObject);
            _roomRoot = null;
        }
    }

    private void OnApplicationQuit()
    {
        Clear();
    }
}
