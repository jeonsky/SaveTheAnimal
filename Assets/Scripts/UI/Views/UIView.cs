using UnityEngine;

/// <summary>화면 하나(타이틀, HUD, 팝업 등)의 공통 베이스</summary>
public abstract class UIView
{
    public RectTransform Root { get; }
    protected readonly UIManager ui;
    readonly CanvasGroup group;

    protected UIView(RectTransform parent, UIManager ui, string name)
    {
        this.ui = ui;
        Root = UIFactory.CreateRect(name, parent).Stretch();
        group = Root.gameObject.AddComponent<CanvasGroup>();
        Root.gameObject.SetActive(false);
    }

    public bool IsVisible => Root.gameObject.activeSelf;

    public void Show()
    {
        if (IsVisible) return;
        Root.gameObject.SetActive(true);
        group.alpha = 0f;
        ui.Run(UITween.Fade(group, 1f, 0.15f));
        OnShow();
    }

    public void Hide()
    {
        if (!IsVisible) return;
        Root.gameObject.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        if (visible) Show();
        else Hide();
    }

    /// <summary>매 프레임 호출 (보이는 동안만)</summary>
    public void Tick()
    {
        if (IsVisible) OnTick();
    }

    protected virtual void OnShow() { }
    protected virtual void OnTick() { }

    /// <summary>뒤 화면 터치를 막는 반투명 배경</summary>
    protected void AddBlocker(Color color)
    {
        var bg = UIFactory.Img(Root, "Blocker", color);
        bg.rectTransform.Stretch();
        bg.raycastTarget = true;
    }
}
