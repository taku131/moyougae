using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GLTFast;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Threading.Tasks;


public class FurnitureManager : MonoBehaviour
{
    [Serializable]
    public class FurnitureItem
    {
        public int id;
        public string name;
        public GameObject prefab;      // 既存プレハブ用
        public Sprite thumbnail;
        public bool isGenerated;

        // ★生成モデル（GLB）のローカル保存先
        public string localGlbPath;
    }

    [Header("Spawn Settings")]
    public Camera mainCamera;
    public Transform furnitureParent;

    [Header("UI Catalog")]
    public Transform buttonParent;
    public Button buttonTemplate;

    [Header("Initial Items (既存プレハブ)")]
    public List<FurnitureItem> items = new List<FurnitureItem>();

    [Header("Spawn Anchor")]
    public Transform rightHandAnchor;
    public float handForwardOffset = 0.25f;
    public float handUpOffset = 0.0f;

    [Header("Context UI (always on)")]
    public FurnitureContextUI contextUI;

    [Header("Auto select grabbed object")]
    public bool autoSelectGrabbed = true;
    private GameObject _lastGrabbed;



    private GameObject current;
    private int nextId = 0;

    // ===== 永続化 =====
    [Serializable] private class SavedGeneratedItem { public string name; public string fileName; }
    [Serializable] private class SavedGeneratedList { public List<SavedGeneratedItem> items = new List<SavedGeneratedItem>(); }

    private string GeneratedDir => System.IO.Path.Combine(Application.persistentDataPath, "GeneratedModels");
    private string CatalogJsonPath => System.IO.Path.Combine(Application.persistentDataPath, "generated_catalog.json");

    private void Awake()
    {
        // 既存プレハブのIDを振る
        for (int i = 0; i < items.Count; i++)
            items[i].id = nextId++;

        RebuildButtons();
    }

    private void Start()
    {
        // 配置物だけ消す（itemsは消さない）
        ClearAllFurniture();

        if (contextUI != null)
            contextUI.Init(this);

        SetSelected(null);

        // ★保存済み生成モデルを items に復元
        LoadGeneratedCatalog();
    }

    public void ClearAllFurniture()
    {
        if (furnitureParent == null) return;

        foreach (Transform child in furnitureParent)
            Destroy(child.gameObject);

        current = null;
        SetSelected(null);
    }

    // ===== 生成モデルの登録（永続化あり） =====
    public int RegisterGeneratedFurnitureFromFile(string localGlbPath, string displayName)
    {
        if (string.IsNullOrEmpty(localGlbPath) || !System.IO.File.Exists(localGlbPath))
        {
            Debug.LogError($"[Catalog] glb not found: {localGlbPath}");
            return -1;
        }

        // 重複（同じファイル）を防止
        string fn = System.IO.Path.GetFileName(localGlbPath);
        bool exists = items.Exists(x => x.isGenerated && System.IO.Path.GetFileName(x.localGlbPath) == fn);
        if (exists)
        {
            Debug.LogWarning($"[Catalog] already exists: {fn}");
            return -1;
        }

        var item = new FurnitureItem
        {
            id = nextId++,
            name = displayName,
            prefab = null,
            thumbnail = null,
            isGenerated = true,
            localGlbPath = localGlbPath
        };

        items.Add(item);
        CreateButtonForItem(item);
        SaveGeneratedCatalog();

        Debug.Log($"[Catalog] Added: {displayName} path={localGlbPath}");
        return item.id;
    }

    private void SaveGeneratedCatalog()
    {
        System.IO.Directory.CreateDirectory(GeneratedDir);

        var list = new SavedGeneratedList();
        foreach (var it in items)
        {
            if (!it.isGenerated) continue;
            if (string.IsNullOrEmpty(it.localGlbPath)) continue;
            if (!System.IO.File.Exists(it.localGlbPath)) continue;

            list.items.Add(new SavedGeneratedItem
            {
                name = it.name,
                fileName = System.IO.Path.GetFileName(it.localGlbPath)
            });
        }

        var json = JsonUtility.ToJson(list, prettyPrint: true);
        System.IO.File.WriteAllText(CatalogJsonPath, json);
        Debug.Log($"[Catalog] Saved: {CatalogJsonPath}");
    }

    private void LoadGeneratedCatalog()
    {
        System.IO.Directory.CreateDirectory(GeneratedDir);

        if (!System.IO.File.Exists(CatalogJsonPath))
        {
            Debug.Log("[Catalog] No saved catalog yet");
            return;
        }

        var json = System.IO.File.ReadAllText(CatalogJsonPath);
        var list = JsonUtility.FromJson<SavedGeneratedList>(json);
        if (list == null || list.items == null) return;

        int added = 0;
        foreach (var s in list.items)
        {
            var path = System.IO.Path.Combine(GeneratedDir, s.fileName);
            if (!System.IO.File.Exists(path)) continue;

            bool exists = items.Exists(x => x.isGenerated && System.IO.Path.GetFileName(x.localGlbPath) == s.fileName);
            if (exists) continue;

            var item = new FurnitureItem
            {
                id = nextId++,
                name = s.name,
                prefab = null,
                thumbnail = null,
                isGenerated = true,
                localGlbPath = path
            };

            items.Add(item);
            CreateButtonForItem(item);
            added++;
        }

        Debug.Log($"[Catalog] Loaded generated items: {added}");
    }

    // ===== Spawn =====
    public FurnitureItem GetItemById(int id) => items.Find(x => x.id == id);

    public void SpawnFurnitureById(int id)
    {
        var data = GetItemById(id);
        if (data == null) return;

        // ★生成モデル（GLBファイル）なら、ファイルからロードして生成
        if (data.isGenerated && data.prefab == null && !string.IsNullOrEmpty(data.localGlbPath))
        {
            StartCoroutine(SpawnFromLocalGlb(data.localGlbPath, data.name));
            return;
        }

        // ★通常プレハブ
        if (data.prefab == null) return;

        Transform t = rightHandAnchor != null ? rightHandAnchor : mainCamera.transform;
        Vector3 pos = t.position + t.forward * handForwardOffset + t.up * handUpOffset;

        Quaternion rot = Quaternion.LookRotation(
            new Vector3(t.forward.x, 0f, t.forward.z).normalized,
            Vector3.up
        );

        current = Instantiate(data.prefab, pos, rot, furnitureParent);
        NormalizeSize(current, 0.8f);
        FurnitureAutoSetup.MakeGrabbable(current);
        SetSelected(current);
    }

    private IEnumerator SpawnFromLocalGlb(string glbPath, string displayName)
    {
        if (!System.IO.File.Exists(glbPath))
        {
            Debug.LogError($"[Spawn] glb missing: {glbPath}");
            yield break;
        }

        Transform t = rightHandAnchor != null ? rightHandAnchor : mainCamera.transform;
        Vector3 pos = t.position + t.forward * handForwardOffset + t.up * handUpOffset;

        var root = new GameObject(displayName);
        root.transform.SetParent(furnitureParent, false);
        root.transform.SetPositionAndRotation(pos, Quaternion.identity);

        var gltf = new GltfImport();
        var bytes = System.IO.File.ReadAllBytes(glbPath);

        // ---- Load (bool / Task<bool> 両対応) ----
        bool loadOk = false;
        Exception loadEx = null;

        object loadOp = gltf.LoadGltfBinary(bytes); // ←ここがboolの環境がある
        yield return AwaitBool(loadOp, ok => loadOk = ok, ex => loadEx = ex);

        if (loadEx != null || !loadOk)
        {
            Debug.LogError($"[Spawn] glTF load failed: {loadEx}");
            Destroy(root);
            yield break;
        }

        // ---- Instantiate (bool / Task<bool> 両対応) ----
        bool instOk = false;
        Exception instEx = null;

        object instOp = gltf.InstantiateMainScene(root.transform); // ←ここもboolの環境がある
        yield return AwaitBool(instOp, ok => instOk = ok, ex => instEx = ex);

        if (instEx != null || !instOk)
        {
            Debug.LogError($"[Spawn] glTF instantiate failed: {instEx}");
            Destroy(root);
            yield break;
        }

        

        //glTF 読み込み完了
        yield return null; // ★1フレーム待つ（超重要なことがある）
        QuestMaterialFixer.FixMaterialsForQuest(root);

        MaterialTextureDumper.Dump(root, "BEFORE");
        GltfUrpMaterialFixer.ReplaceToUrpLitKeepingTextures(root);
        MaterialTextureDumper.Dump(root, "AFTER");
        Debug.Log("[TEXDUMP] Instantiate OK. start dump...");
        




        NormalizeSize(root, 0.8f);
        FurnitureAutoSetup.MakeGrabbable(root);
        SetSelected(root);
    }


    private void NormalizeSize(GameObject go, float maxSizeMeters = 0.8f)
    {
        if (go == null) return;

        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float largest = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (largest <= 0.0001f) return;

        float scale = maxSizeMeters / largest;

        // ★正規化（初期サイズ決定）
        go.transform.localScale *= scale;

        // ★正規化後のスケールを保存（ここが重要）
        var ss = go.GetComponent<FurnitureScaleState>();
        if (ss == null) ss = go.AddComponent<FurnitureScaleState>();
        ss.baseScale = go.transform.localScale;
        ss.normalizedOnce = true;
    }


    // ===== Selection / UI =====
    public void SetSelected(GameObject obj)
    {
        current = obj;

        if (contextUI != null)
            contextUI.SetTarget(current);

        Debug.Log($"[Select] {(current ? current.name : "null")}");
    }

    private void RebuildButtons()
    {
        if (buttonParent == null || buttonTemplate == null) return;

        foreach (Transform child in buttonParent)
        {
            if (child == buttonTemplate.transform) continue;
            Destroy(child.gameObject);
        }

        foreach (var item in items)
            CreateButtonForItem(item);

        buttonTemplate.gameObject.SetActive(false);
    }

    private void CreateButtonForItem(FurnitureItem item)
    {
        var btnObj = Instantiate(buttonTemplate.gameObject, buttonParent);
        btnObj.SetActive(true);

        var btn = btnObj.GetComponent<Button>();

        var texts = btnObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        if (texts.Length > 0) texts[0].text = item.name;

        var img = btnObj.GetComponentInChildren<Image>(true);
        if (img != null && item.thumbnail != null) img.sprite = item.thumbnail;

        btn.onClick.RemoveAllListeners();
        int idCopy = item.id;

        btn.onClick.AddListener(() =>
        {
            Debug.Log($"Clicked item id={idCopy} name={item.name}");
            SpawnFurnitureById(idCopy);
        });

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)buttonParent);
    }

    // ===== Actions =====
    public void TogglePinCurrent()
    {
        if (current == null) return;

        var state = current.GetComponent<FurnitureState>();
        if (state == null) state = current.AddComponent<FurnitureState>();

        state.ApplyPinned(!state.pinned);
        Debug.Log($"[Furniture] pinned = {state.pinned}");

        if (contextUI != null)
            contextUI.SetTarget(current);
    }

    public void DeleteCurrent()
    {
        if (current == null) return;

        var toDelete = current;
        current = null;

        if (contextUI != null)
            contextUI.SetTarget(null);

        Destroy(toDelete);
        Debug.Log("[Furniture] Deleted");
    }

    public void SetCurrentUniformScale(float multiplier)
    {
        if (current == null) return;

        var ss = current.GetComponent<FurnitureScaleState>();
        if (ss == null)
        {
            ss = current.AddComponent<FurnitureScaleState>();
            ss.baseScale = current.transform.localScale;
        }

        ss.multiplier = Mathf.Clamp(multiplier, 0.1f, 10.0f);
        current.transform.localScale = ss.baseScale * ss.multiplier;

        if (contextUI != null)
            contextUI.SetTarget(current);
    }



    // ===== Auto select grabbed =====
    private void Update()
    {
        if (!autoSelectGrabbed) return;

        if (TryGetGrabbedFurniture(out var grabbed))
        {
            if (grabbed != _lastGrabbed)
            {
                _lastGrabbed = grabbed;
                SetSelected(grabbed);
            }
        }
    }

    private bool TryGetGrabbedFurniture(out GameObject grabbedRoot)
    {
        grabbedRoot = null;

        var handGrabs = FindObjectsOfType<HandGrabInteractable>(includeInactive: false);
        foreach (var hg in handGrabs)
        {
            if (hg != null && hg.State == InteractableState.Select)
            {
                grabbedRoot = hg.gameObject; // HandGrabInteractableが付いてるのは家具のrootのはず

                return true;
            }
        }

        var grabs = FindObjectsOfType<GrabInteractable>(includeInactive: false);
        foreach (var g in grabs)
        {
            if (g != null && g.State == InteractableState.Select)
            {
                grabbedRoot = g.gameObject; // HandGrabInteractableが付いてるのは家具のrootのはず

                return true;
            }
        }

        return false;
    }

    private static IEnumerator AwaitBool(object op, Action<bool> onDone, Action<Exception> onFail)
    {
        if (op is bool b)
        {
            onDone?.Invoke(b);
            yield break;
        }

        if (op is Task<bool> t)
        {
            while (!t.IsCompleted) yield return null;
            if (t.IsFaulted) onFail?.Invoke(t.Exception);
            else onDone?.Invoke(t.Result);
            yield break;
        }

        onFail?.Invoke(new InvalidOperationException("Unsupported return type: " + (op?.GetType().Name ?? "null")));
    }

}
