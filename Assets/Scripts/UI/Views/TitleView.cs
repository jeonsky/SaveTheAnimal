using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>타이틀 화면: 로고, 동물 소개, 하트 현황, START / COLLECTION 버튼</summary>
public class TitleView : UIView
{
    readonly HeartsWidget hearts;
    readonly Button startButton;
    readonly TextMeshProUGUI hint;
    readonly RectTransform animalRow;
    bool animalsBuilt;

    public TitleView(RectTransform parent, UIManager ui) : base(parent, ui, "Title")
    {
        AddBlocker(UIFactory.Paper);

        var logo = UIFactory.Label(Root, "SAVE THE\nANIMAL", 150, UIFactory.Orange, true);
        logo.rectTransform.Center(0, 560, 1000, 380);
        logo.lineSpacing = -10f;

        var sub = UIFactory.Label(Root, "Match 3 to rescue the animals!", 46, UIFactory.SubInk);
        sub.rectTransform.Center(0, 330, 1000, 70);

        animalRow = UIFactory.CreateRect("Animals", Root).Center(0, 170, 1000, 170);

        var heartCard = UIFactory.Img(Root, "HeartCard", new Color(1f, 1f, 1f, 0.85f), UIFactory.Rounded);
        heartCard.rectTransform.Center(0, -40, 720, 220);
        hearts = new HeartsWidget(heartCard.transform, 90f);
        hearts.Root.Stretch(0, 20, 0, 36);

        startButton = UIFactory.MakeButton(Root, "START", UIFactory.Green, ui.StartGame, 72);
        startButton.RT().Center(0, -290, 640, 170);

        hint = UIFactory.Label(Root, "", 36, UIFactory.SubInk);
        hint.rectTransform.Center(0, -400, 1000, 50);

        var collectionButton = UIFactory.MakeButton(Root, "COLLECTION", UIFactory.Blue, ui.OpenCollection, 54);
        collectionButton.RT().Center(0, -530, 640, 140);
    }

    protected override void OnShow()
    {
        BuildAnimalsOnce();
        OnTick();
        ui.Run(UITween.PopIn(startButton.transform));
    }

    protected override void OnTick()
    {
        hearts.Refresh();

        bool hasHeart = HeartSystem.Hearts > 0;
        startButton.interactable = hasHeart;
        hint.text = hasHeart
            ? "Uses 1 heart (returned if you clear!)"
            : "No hearts left. Wait for a refill!";
    }

    void BuildAnimalsOnce()
    {
        if (animalsBuilt) return;

        Sprite[] sprites = BoardManager.instance != null ? BoardManager.instance.animalSprites : null;
        if (sprites == null || sprites.Length == 0) return;
        animalsBuilt = true;

        const float size = 150f, gap = 30f;
        float total = sprites.Length * size + (sprites.Length - 1) * gap;

        for (int i = 0; i < sprites.Length; i++)
        {
            Image icon = UIFactory.Img(animalRow, "Animal" + i, Color.white, sprites[i]);
            icon.preserveAspect = true;
            icon.rectTransform.Center(-total / 2f + size / 2f + i * (size + gap), 0, size, size);
        }
    }
}
