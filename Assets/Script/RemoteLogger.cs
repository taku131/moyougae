using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class RemoteLogger : MonoBehaviour
{
    public string baseUrl;   // https://xxxx.ngrok-free.dev
    public string sid;       // session_id

    void OnEnable() => Application.logMessageReceived += OnLog;
    void OnDisable() => Application.logMessageReceived -= OnLog;

    void OnLog(string condition, string stackTrace, LogType type)
    {
        // •K—v‚È‚ç type==Error ‚¾‚¯‘—‚é‚È‚Ç
        StartCoroutine(PostLog($"{type}: {condition}"));
    }

    IEnumerator PostLog(string msg)
    {
        var form = new WWWForm();
        form.AddField("msg", msg);
        using var req = UnityWebRequest.Post($"{baseUrl}/client-log/{sid}", form);
        yield return req.SendWebRequest();
    }
}
