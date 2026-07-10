using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class CaptureToModel : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] string baseUrl = "https://xxxxx.ngrok-free.dev"; // https必須

    [Header("UI / Placement")]
    [SerializeField] TMP_Text codeText;
    [SerializeField] Transform spawnPoint; // ※もう表示しないので実質未使用（残してOK）

    [Header("Timeout / Polling")]
    [SerializeField] float waitTimeoutSeconds = 300f;
    [SerializeField] float pollIntervalSeconds = 2f;

    [Header("Optional: loading icon")]
    [SerializeField] GameObject loadingIcon;

    [Header("Register (NO INSTANTIATE)")]
    [SerializeField] GeneratedModelImporter importer;  // ★Inspectorで刺す！

    string sessionId;
    string sixCode;

    Coroutine flowRoutine;
    bool cancelled;

    void OnEnable()
    {
        cancelled = false;
        flowRoutine = StartCoroutine(Flow());
    }

    void OnDisable() { CancelFlow(); }
    void OnDestroy() { CancelFlow(); }

    void CancelFlow()
    {
        cancelled = true;
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }
        SetLoading(false);
    }

    void SetLoading(bool on)
    {
        if (loadingIcon != null) loadingIcon.SetActive(on);
    }

    IEnumerator Flow()
    {
        Debug.Log($"[Flow] baseUrl={baseUrl}");

        if (string.IsNullOrEmpty(baseUrl) || !baseUrl.StartsWith("https"))
        {
            Debug.LogError("[Flow] baseUrl は https を指定してください（Androidはhttp不可）");
            if (codeText) codeText.text = "ERROR: baseUrl";
            yield break;
        }

        // ===== 1) セッション作成 =====
        var createUrl = $"{baseUrl}/sessions";
        using (var req = UnityWebRequest.Post(createUrl, new WWWForm()))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();
            if (cancelled) yield break;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Flow] /sessions 失敗: {req.error}\nBody:{req.downloadHandler?.text}");
                if (codeText) codeText.text = "ERROR: /sessions";
                yield break;
            }

            var jsonText = req.downloadHandler.text;
            var json = JsonUtility.FromJson<CreateRes>(jsonText);

            if (string.IsNullOrEmpty(json.session_id) || string.IsNullOrEmpty(json.code))
            {
                Debug.LogError($"[Flow] /sessions JSON不正: {jsonText}");
                if (codeText) codeText.text = "ERROR: JSON";
                yield break;
            }

            sessionId = json.session_id;
            sixCode = json.code;

            if (codeText) codeText.text = sixCode;
            Debug.Log($"[Flow] session={sessionId}, code={sixCode}");
        }

        // ===== 2) done待ち =====
        SetLoading(true);

        var statusUrl = $"{baseUrl}/sessions/{sessionId}/status";
        float startTime = Time.realtimeSinceStartup;

        while (!cancelled)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed >= waitTimeoutSeconds)
            {
                Debug.LogWarning($"[Flow] TIMEOUT: waited {elapsed:F1}s");
                if (codeText) codeText.text = $"{sixCode}\nTIMEOUT";
                SetLoading(false);
                yield break;
            }

            using (var req = UnityWebRequest.Get(statusUrl))
            {
                req.timeout = 15;
                yield return req.SendWebRequest();
                if (cancelled) yield break;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var st = JsonUtility.FromJson<StatusRes>(req.downloadHandler.text);

                    if (st.status == "done" && !string.IsNullOrEmpty(st.model_url))
                    {
                        // ★名前を拾う（サーバがどっちで返してても対応）
                        string name =
                            !string.IsNullOrEmpty(st.model_name) ? st.model_name :
                            !string.IsNullOrEmpty(st.display_name) ? st.display_name :
                            "Generated";

                        // ★Importして「FurnitureManagerに登録」だけする（表示しない）
                        if (importer == null)
                        {
                            Debug.LogError("[Flow] importer が null です。CaptureToModel の Inspector で GeneratedModelImporter を刺して！");
                            if (codeText) codeText.text = $"{sixCode}\nIMPORTER NULL";
                            SetLoading(false);
                            yield break;
                        }

                        string fullUrl = st.model_url.StartsWith("http") ? st.model_url : $"{baseUrl}{st.model_url}";
                        Debug.Log($"[Flow] DONE -> register only. name={name} url={fullUrl}");

                        importer.ImportFromUrl(fullUrl, name);

                        SetLoading(false);
                        yield break;
                    }

                    if (st.status == "error")
                    {
                        Debug.LogError("[Flow] サーバ側でエラー状態");
                        if (codeText) codeText.text = $"{sixCode}\nERROR";
                        SetLoading(false);
                        yield break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[Flow] /status 取得失敗: {req.error}");
                }
            }

            yield return new WaitForSeconds(pollIntervalSeconds);
        }
    }

    [System.Serializable]
    struct CreateRes
    {
        public string session_id;
        public string code;
    }

    [System.Serializable]
    struct StatusRes
    {
        public string status;
        public string model_url;

        // ★サーバが返す名前フィールドに合わせてどっちでもOKにする
        public string model_name;
        public string display_name;
    }
}
