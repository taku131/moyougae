using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FurnitureContextUI : MonoBehaviour
{
    public Button pinButton;
    public Button deleteButton;

    public TMP_Text titleText;
    public TMP_Text pinButtonText;

    [Header("Scale UI")]
    public Slider scaleSlider;          // ★追加
    public TMP_Text scaleValueText;     // 任意：数値表示 例「1.00」

    private FurnitureManager _manager;
    private GameObject _target;



    public void Init(FurnitureManager manager)
    {
        _manager = manager;

        pinButton.onClick.RemoveAllListeners();
        deleteButton.onClick.RemoveAllListeners();

        pinButton.onClick.AddListener(() => _manager.TogglePinCurrent());
        deleteButton.onClick.AddListener(() => _manager.DeleteCurrent());

        // ★スライダーイベント
        if (scaleSlider != null)
        {
            scaleSlider.onValueChanged.RemoveAllListeners();
            scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        }

        SetTarget(null);
    }

    public void SetTarget(GameObject target)
    {
        _target = target;
        bool hasTarget = _target != null;

        pinButton.interactable = hasTarget;
        deleteButton.interactable = hasTarget;

        if (titleText != null)
            titleText.text = hasTarget ? _target.name : "未選択";

        // ピン文言
        if (pinButtonText != null)
        {
            bool pinned = false;
            if (hasTarget)
            {
                var state = _target.GetComponent<FurnitureState>();
                pinned = state != null && state.pinned;
            }
            pinButtonText.text = pinned ? "固定解除" : "固定";
        }

        // ★対象が変わったらスライダーを現在スケールに合わせる
        if (scaleSlider != null)
        {
            scaleSlider.interactable = hasTarget;

            if (hasTarget)
            {
                // uniform前提：xを見る
                var ss = _target.GetComponent<FurnitureScaleState>();
                float mul = 1f;
                if (ss != null && ss.baseScale.x != 0f)
                    mul = _target.transform.localScale.x / ss.baseScale.x;

                scaleSlider.SetValueWithoutNotify(mul);
                UpdateScaleText(mul);

            }
            else
            {
                UpdateScaleText(0f);
            }
        }
    }

    private void OnScaleChanged(float value)
    {
        // 対象がないなら何もしない
        if (_target == null) return;

        _manager.SetCurrentUniformScale(value);
        UpdateScaleText(value);
    }

    private void UpdateScaleText(float value)
    {
        if (scaleValueText != null)
            scaleValueText.text = value <= 0f ? "-" : value.ToString("0.00");
    }
}
