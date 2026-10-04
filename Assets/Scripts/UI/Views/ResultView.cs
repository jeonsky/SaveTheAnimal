using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>결과 팝업: 클리어/실패, 이번 판에 구조한 동물, 하트 현황, 다시하기/홈/도감</summary>
public class ResultView : UIView
{
    readonly RectTransform card;
    readonly TextMeshProUGUI title;
    readonly TextMeshProUGUI score;
    readonly TextMeshProUGUI note;
    readonly RectTransform animalRow;
    readonly HeartsWidget hearts;
    readonly Button retryButton;

    public ResultView(RectTransform parent, UIManager ui) : base(parent, ui, "Result")
    {
        AddBlocker(UIFactory.Dim);

        card = UIFactory.Img(Root, "Card", UIFactory.Cream, UIFactory.Rounded).rectTransform.Center(0, 0, 880, 1320);

        title = UIFactory.Label(card, "", 88, UIFactory.Green, true);
        title.rectTransform.Center(0, 540, 820, 120);
        title.enableAutoSizing = true;
        title.fontSizeMin = 40;
        title.fontSizeMax = 88;

        score = UIFactory.Label(card, "", 56, UIFactory.Ink, true);
        score.rectTransform.Center(0, 420, 820, 80);

        note = UIFactory.Label(card, "", 38, UIFactory.SubInk);
        note.rectTransform.Center(0, 345, 820, 60);

        UIFactory.Label(card, "RESCUED ANIMALS", 36, UIFactory.SubInk, true).rectTransform.Center(0, 255, 820, 50);
        animalRow = UIFactory.CreateRect("Animals", card).Center(0, 110, 820, 220);

        hearts = new HeartsWidget(card, 72f);
        hearts.Root.Center(0, -60, 700, 140);

        retryButton = UIFactory.MakeButton(card, "RETRY", UIFactory.Green, ui.RetryGame, 60);
        retryButton.RT().Center(0, -270, 640, 150);

        UIFactory.MakeButton(card, "HOME", UIFactory.Gray, ui.GoHome, 50).RT().Center(-165, -450, 310, 130);
        UIFactory.MakeButton(card, "COLLECTION", UIFactory.Blue, ui.OpenCollection, 40).RT().Center(165, -450, 310, 130);
    }

    protected override void OnShow()
    {
        StageManager stage = StageManager.instance;
        bool cleared = stage.IsCleared;

        title.text = cleared ? "MISSION COMPLETE!" : "TIME OUT!";
        title.color = cleared ? UIFactory.Green : UIFactory.Red;
        score.text = $"Rescued {stage.currentRescueCount} / {stage.targetRescueCount}";
        note.text = cleared
            ? $"{Mathf.CeilToInt(stage.currentTime)}s left  -  Your heart is back!"
            : "You lost a heart. Try again!";

        BuildAnimalRow(stage);
        OnTick();
        ui.Run(UITween.PopIn(card));
    }

    protected override void OnTick()
    {
        hearts.Refresh();
        retryButton.interactable = HeartSystem.Hearts > 0;
    }

    void BuildAnimalRow(StageManager stage)
    {
        foreach (Transform child in animalRow)
        {
            Object.Destroy(child.gameObject);
        }

        Sprite[] sprites = BoardManager.instance != null ? BoardManager.instance.animalSprites : null;
        if (sprites == null || sprites.Length == 0) return;

        float slotWidth = 820f / sprites.Length;

        for (int i = 0; i < sprites.Length; i++)
        {
            int count = stage.GetRescued(i);
            float x = -410f + slotWidth * (i + 0.5f);

            RectTransform slot = UIFactory.CreateRect("Slot" + i, animalRow).Center(x, 0, slotWidth, 220);

            Image icon = UIFactory.Img(slot, "Icon", count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.3f), sprites[i]);
            icon.preserveAspect = true;
            icon.rectTransform.Center(0, 30, 120, 120);

            UIFactory.Label(slot, "x " + count, 42, count > 0 ? UIFactory.Ink : UIFactory.Gray, true)
                .rectTransform.Center(0, -65, slotWidth, 50);

            if (stage.IsNewDiscovery(i))
            {
                Image badge = UIFactory.Img(slot, "NewBadge", UIFactory.Orange, UIFactory.Rounded);
                badge.rectTransform.Center(38, 95, 96, 44);
                UIFactory.Label(badge.transform, "NEW", 28, Color.white, true).rectTransform.Stretch();
                ui.Run(UITween.PopIn(badge.transform, 0.4f));
            }
        }
    }
}
