using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildProgressUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject rootPanel;     // Panel全体
    public TMP_Text statusText;      // TextMeshPro
    public Slider progressBar;       // 0..1

    [Header("Behavior")]
    public float fakeDuration = 1.2f;  // 最低でもこれくらいロード演出する
    public float doneHold = 0.8f;

    Coroutine _co;

    void Awake()
    {
        SetVisible(false);
        SetProgress(0f);
    }

    public void Begin(string message = "生成中…")
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoBegin(message));
    }

    IEnumerator CoBegin(string message)
    {
        SetVisible(true);
        SetStatus(message);
        SetProgress(0f);

        // “動いてる感”のための疑似進捗（0→0.9まで）
        float t = 0f;
        while (t < fakeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fakeDuration);
            // 0.9まで
            SetProgress(Mathf.Lerp(0f, 0.9f, p));
            yield return null;
        }
    }

    public void Finish(string message = "完了！")
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFinish(message));
    }

    IEnumerator CoFinish(string message)
    {
        SetStatus(message);
        SetProgress(1f);
        yield return new WaitForSeconds(doneHold);
        SetVisible(false);
    }

    public void Fail(string message = "失敗しました")
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFail(message));
    }

    IEnumerator CoFail(string message)
    {
        SetStatus(message);
        SetProgress(0f);
        // 失敗は見えるように少し長め
        yield return new WaitForSeconds(2.0f);
        SetVisible(false);
    }

    void SetVisible(bool on)
    {
        if (rootPanel != null) rootPanel.SetActive(on);
        else gameObject.SetActive(on);
    }

    void SetStatus(string s)
    {
        if (statusText != null) statusText.text = s;
    }

    void SetProgress(float v)
    {
        if (progressBar != null) progressBar.value = v;
    }
}
