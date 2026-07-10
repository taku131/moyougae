using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GeneratedModelImporter : MonoBehaviour
{
    public FurnitureManager furnitureManager;

    // サーバから返ってきたURLをここに渡すだけ
    public void ImportFromUrl(string glbUrl, string displayName = "Generated")
    {
        StartCoroutine(ImportCoroutine(glbUrl, displayName));
    }

    private IEnumerator ImportCoroutine(string glbUrl, string displayName)
    {
        if (furnitureManager == null)
        {
            Debug.LogError("[Import] FurnitureManager is null");
            yield break;
        }
        if (string.IsNullOrEmpty(glbUrl))
        {
            Debug.LogError("[Import] glbUrl is empty");
            yield break;
        }

        Debug.Log($"[Import] Downloading: {glbUrl}");

        using var req = UnityWebRequest.Get(glbUrl);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 60;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Import] Download failed: {req.error}");
            yield break;
        }

        byte[] data = req.downloadHandler.data;
        Debug.Log($"[Import] Download ok bytes={data.Length}");

        // ★GLBをQuest内に保存
        string dir = System.IO.Path.Combine(Application.persistentDataPath, "GeneratedModels");
        System.IO.Directory.CreateDirectory(dir);

        // 文字として安全なファイル名にする（空白とか記号で壊れないように）
        string safeName = MakeSafeFileName(displayName);
        string fileName = $"{System.DateTime.Now:yyyyMMdd_HHmmss}_{safeName}.glb";
        string savePath = System.IO.Path.Combine(dir, fileName);

        System.IO.File.WriteAllBytes(savePath, data);

        // ★FurnitureManagerに「ファイルとして」登録（ここでは表示しない）
        int id = furnitureManager.RegisterGeneratedFurnitureFromFile(savePath, displayName);
        Debug.Log($"[Import] Saved & Registered id={id} path={savePath}");
    }

    private static string MakeSafeFileName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "Generated";
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }
}
