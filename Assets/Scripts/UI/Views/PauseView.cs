using UnityEngine;
using UnityEngine.UI;

/// <summary>일시정지 팝업: 계속하기 / 다시하기 / 홈</summary>
public class PauseView : UIView
{
    readonly RectTransform card;
    readonly Button retryButton;

    public PauseView(RectTransform parent, UIManager ui) : base(parent, ui, "Pause")
    {
        AddBlocker(UIFactory.Dim);

        card = UIFactory.Img(Root, "Card", UIFactory.Cream, UIFactory.Rounded).rectTransform.Center(0, 0, 760, 860);

        UIFactory.Label(card, "PAUSED", 96, UIFactory.Ink, true).rectTransform.Center(0, 300, 700, 120);

        UIFactory.MakeButton(card, "RESUME", UIFactory.Green, ui.ResumeGame, 60).RT().Center(0, 120, 560, 150);

        retryButton = UIFactory.MakeButton(card, "RETRY", UIFactory.Orange, ui.RetryGame, 60);
        retryButton.RT().Center(0, -60, 560, 150);

        UIFactory.Label(card, "Retry uses 1 more heart", 34, UIFactory.SubInk).rectTransform.Center(0, -165, 700, 50);

        UIFactory.MakeButton(card, "HOME", UIFactory.Gray, ui.GoHome, 60).RT().Center(0, -290, 560, 150);
    }

    protected override void OnShow()
    {
        OnTick();
        ui.Run(UITween.PopIn(card));
    }

    protected override void OnTick()
    {
        retryButton.interactable = HeartSystem.Hearts > 0;
    }
}
