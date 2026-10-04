using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>버튼을 누르는 동안 살짝 작아지는 터치 피드백</summary>
public class ButtonPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    Selectable selectable;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable == null || selectable.IsInteractable())
        {
            transform.localScale = Vector3.one * 0.94f;
        }
    }

    public void OnPointerUp(PointerEventData eventData) => transform.localScale = Vector3.one;

    public void OnPointerExit(PointerEventData eventData) => transform.localScale = Vector3.one;
}
