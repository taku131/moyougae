using UnityEngine;
using UnityEngine.UI;

public class FurnitureScaleSlider : MonoBehaviour
{
    public Slider slider;
    public Transform target;   // サイズを変えたい家具
    public float minScale = 0.5f;
    public float maxScale = 5.0f;

    Vector3 baseScale;

    void Start()
    {
        if (target == null) return;

        baseScale = target.localScale;

        slider.onValueChanged.AddListener(OnValueChanged);
    }

    void OnValueChanged(float v)
    {
        float t = slider.normalizedValue; // 0〜1
        float scale = Mathf.Lerp(minScale, maxScale, t);

        target.localScale = baseScale * scale;

        Debug.Log($"[Slider] v={v}, scale={scale}");
    }
}
