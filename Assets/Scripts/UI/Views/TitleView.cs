using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>타이틀 화면: 로고, 현재 스테이지, 이번 판 동물, 하트 현황, START / SHELTER / YARD 버튼</summary>
public class TitleView : UIView
{
    readonly HeartsWidget hearts;
    readonly Button startButton;
    readonly TextMeshProUGUI hint;
    readonly TextMeshProUGUI stageLabel;
    readonly RectTransform animalRow;
    readonly Image adoptBadge;
    readonly TextMeshProUGUI adoptBadgeText;
    readonly Image yardBadge;
    readonly TextMeshProUGUI yardBadgeText;

    public TitleView(RectTransform parent, UIManager ui) : base(parent, ui, "Title")
    {
        AddBlocker(UIFactory.Paper);

        var logo = UIFactory.Label(Root, "SAVE THE\nANIMAL", 150, UIFactory.Orange, true);
        logo.rectTransform.Center(0, 590, 1000, 360);
        logo.lineSpacing = -10f;

        var sub = UIFactory.Label(Root, "Rescue them, then find them a home!", 44, UIFactory.SubInk);
        sub.rectTransform.Center(0, 375, 1000, 64);

        var stagePill = UIFactory.Img(Root, "StagePill", UIFactory.Orange, UIFactory.Rounded);
        stagePill.rectTransform.Center(0, 280, 300, 72);
        stageLabel = UIFactory.Label(stagePill.transform, "", 44, Color.white, true);
        stageLabel.rectTransform.Stretch();

        animalRow = UIFactory.CreateRect("Animals", Root).Center(0, 150, 1000, 150);

        var heartCard = UIFactory.Img(Root, "HeartCard", new Color(1f, 1f, 1f, 0.85f), UIFactory.Rounded);
        heartCard.rectTransform.Center(0, -60, 720, 210);
        hearts = new HeartsWidget(heartCard.transform, 86f);
        hearts.Root.Stretch(0, 16, 0, 30);

        startButton = UIFactory.MakeButton(Root, "START", UIFactory.Green, ui.StartGame, 72);
        startButton.RT().Center(0, -300, 640, 170);

        hint = UIFactory.Label(Root, "", 36, UIFactory.SubInk);
        hint.rectTransform.Center(0, -410, 1000, 50);

        // 왼쪽: SHELTER (도감 & 입양 보내기) / 오른쪽: YARD (입양한 친구들이 노는 마당)
        var collectionButton = UIFactory.MakeButton(Root, "SHELTER", UIFactory.Blue, ui.OpenCollection, 48);
        collectionButton.RT().Center(-168, -540, 304, 140);

        adoptBadge = UIFactory.Img(collectionButton.transform, "AdoptBadge", UIFactory.Red, UIFactory.Rounded);
        adoptBadge.rectTransform.Place(new Vector2(1f, 1f), new Vector2(12, 12), new Vector2(64, 64));
        adoptBadgeText = UIFactory.Label(adoptBadge.transform, "", 32, Color.white, true);
        adoptBadgeText.rectTransform.Stretch();
        adoptBadge.gameObject.SetActive(false);

        var yardButton = UIFactory.MakeButton(Root, "YARD", UIFactory.Orange, ui.OpenYard, 48);
        yardButton.RT().Center(168, -540, 304, 140);

        yardBadge = UIFactory.Img(yardButton.transform, "YardBadge", UIFactory.Green, UIFactory.Rounded);
        yardBadge.rectTransform.Place(new Vector2(1f, 1f), new Vector2(12, 12), new Vector2(64, 64));
        yardBadgeText = UIFactory.Label(yardBadge.transform, "", 32, Color.white, true);
        yardBadgeText.rectTransform.Stretch();
        yardBadge.gameObject.SetActive(false);
    }

    protected override void OnShow()
    {
        RefreshStageInfo();
        OnTick();
        ui.Run(UITween.PopIn(startButton.transform));
    }

    public void RefreshStageInfo()
    {
        StageManager stage = StageManager.instance;
        if (stage == null) return;

        stageLabel.text = "STAGE " + stage.PlayedStage;
        BuildAnimalRow(stage.Roster);
    }

    protected override void OnTick()
    {
        hearts.Refresh();

        bool hasHeart = HeartSystem.Hearts > 0;
        startButton.interactable = hasHeart;
        hint.text = hasHeart
            ? "Uses 1 heart (returned if you clear!)"
            : "No hearts left. Wait for a refill!";

        int readyCount = CollectionData.CanAdoptCount();
        adoptBadge.gameObject.SetActive(readyCount > 0);
        if (readyCount > 0)
        {
            adoptBadgeText.text = readyCount.ToString();
        }

        int adoptedCount = CollectionData.AdoptedCount();
        yardBadge.gameObject.SetActive(adoptedCount > 0);
        if (adoptedCount > 0)
        {
            yardBadgeText.text = adoptedCount.ToString();
        }
    }

    // 이번 판에 나오는 동물 + 처음 보는 동물에는 NEW 뱃지
    void BuildAnimalRow(Sprite[] roster)
    {
        foreach (Transform child in animalRow) Object.Destroy(child.gameObject);
        if (roster == null || roster.Length == 0) return;

        const float size = 140f, gap = 34f;
        float total = roster.Length * size + (roster.Length - 1) * gap;

        for (int i = 0; i < roster.Length; i++)
        {
            float x = -total / 2f + size / 2f + i * (size + gap);

            Image icon = UIFactory.Img(animalRow, "Animal" + i, Color.white, roster[i]);
            icon.preserveAspect = true;
            icon.rectTransform.Center(x, 0, size, size);

            if (!CollectionData.IsMet(roster[i].name))
            {
                Image badge = UIFactory.Img(animalRow, "NewBadge", UIFactory.Red, UIFactory.Rounded);
                badge.rectTransform.Center(x + 40, 62, 96, 42);
                UIFactory.Label(badge.transform, "NEW", 26, Color.white, true).rectTransform.Stretch();
            }
        }
    }
}
