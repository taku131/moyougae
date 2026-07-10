using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using TMPro;
using GLTFast;

public class CaptureToModelFlow : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] string baseUrl = "http://192.168.x.x:8000"; // ��PC��IP�ɕύX

    [Header("UI / Placement")]
    [SerializeField] TMP_Text codeText;
    [SerializeField] Transform spawnPoint;

    string sessionId;
    string sixCode;

    IEnumerator Start()
    {
        // 1) �Z�b�V�����쐬
        using (var req = UnityWebRequest.PostWwwForm($"{baseUrl}/sessions", ""))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { Debug.LogError(req.error); yield break; }
            var json = JsonUtility.FromJson<CreateRes>(req.downloadHandler.text);
            sessionId = json.session_id;
            sixCode = json.code;
            if (codeText) codeText.text = sixCode;
            Debug.Log($"[Flow] session={sessionId}, code={sixCode}");
        }

        // 2) �X�e�[�^�X�Ď�
        while (true)
        {
            using (var req = UnityWebRequest.Get($"{baseUrl}/sessions/{sessionId}/status"))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var st = JsonUtility.FromJson<StatusRes>(req.downloadHandler.text);
                    if (st.status == "done" && !string.IsNullOrEmpty(st.model_url))
                    {
                        yield return DownloadAndShow(st.model_url);
                        break;
                    }
                    if (st.status == "error") { Debug.LogError("�������s"); break; }
                }
                else Debug.LogWarning(req.error);
            }
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator DownloadAndShow(string relUrl)
    {
        var url = relUrl.StartsWith("http") ? relUrl : $"{baseUrl}{relUrl}";
        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { Debug.LogError(req.error); yield break; }
            var path = Path.Combine(Application.persistentDataPath, $"{sessionId}.glb");
            File.WriteAllBytes(path, req.downloadHandler.data);
            Debug.Log($"[Flow] Saved: {path}");

            var go = new GameObject($"Model_{sessionId}");
            go.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            var asset = go.AddComponent<GltfAsset>();
            asset.Url = $"file://{path}";
        }
    }

    [System.Serializable] struct CreateRes { public string session_id; public string code; }
    [System.Serializable] struct StatusRes { public string status; public string model_url; }
}
