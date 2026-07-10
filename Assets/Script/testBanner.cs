using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class testBanner : MonoBehaviour , IPointerClickHandler
{
    public static UnityEvent<int> Onclicked = new UnityEvent<int>();
    private int myIndex;

    [SerializeField] private TextMeshProUGUI mainText;
    public void Iniitialize(int index)
    {
        mainText.text += $"No{(index + 1):00}";
        myIndex = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Onclicked?.Invoke(myIndex);
    }
}
