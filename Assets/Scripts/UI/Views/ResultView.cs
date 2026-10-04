using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과 팝업: 클리어/실패, 이번 판에 구조한 동물(입양 진행도), 입양 소식,
/// 다음 판에 합류하는 새 친구, 하트 현황, 다음 스테이지/다시하기/홈/도감
/// </summary>
public class ResultView : UIView
{
    readonly RectTransform card;
    readonly TextMeshProUGUI stageLabel;
    readonly TextMeshProUGUI title;
    readonly TextMeshProUGUI score;
    readonly TextMeshProUGUI note;
    readonly RectTransform animalRow;
    readonly RectTransform friendsSection;
    readonly HeartsWidget hearts;
    readonly Button retryButton;
    readonly TextMeshProUGUI retryLabel;

    public ResultView(RectTransform parent, UIManager ui) : base(parent, ui, "Result")
    {
        AddBlocker(UIFactory.Dim);

        card = UIFactory.Img(Root, "Card", UIFactory.Cream, UIFactory.Rounded).rectTransform.Center(0, 0, 880, 1340);

        stageLabel = UIFactory.Label(card, "", 38, UIFactory.SubInk, true);
        stageLabel.rectTransform.Center(0, 600, 820, 50);

        title = UIFactory.Label(card, "", 88, UIFactory.Green, true);
        title.rectTransform.Center(0, 520, 820, 110);
        title.enableAutoSizing = true;
        title.fontSizeMin = 40;
        title.fontSizeMax = 88;

        score = UIFactory.Label(card, "", 54, UIFactory.Ink, true);
        score.rectTransform.Center(0, 425, 820, 70);

        note = UIFactory.Label(card, "", 36, UIFactory.SubInk);
        note.rectTransform.Center(0, 362, 820, 50);
        note.enableAutoSizing = true;
        note.fontSizeMin = 24;
        note.fontSizeMax = 36;

        UIFactory.Label(card, "RESCUED ANIMALS", 34, UIFactory.SubInk, true).rectTransform.Center(0, 290, 820, 44);
        animalRow = UIFactory.CreateRect("Animals", card).Center(0, 150, 820, 240);

        // 새 친구 소식 or 하트 현황 (둘 중 하나만 표시)
        friendsSection = UIFactory.CreateRect("NewFriends", card).Center(0, -80, 820, 190);
        hearts = new HeartsWidget(card, 72f);
        hearts.Root.Center(0, -60, 700, 140);

        retryButton = UIFactory.MakeButton(card, "RETRY", UIFactory.Green, ui.RetryGame, 60);
        retryButton.RT().Center(0, -290, 640, 150);
        retryLabel = retryButton.GetComponentInChildren<TextMeshProUGUI>();

        UIFactory.MakeButton(card, "HOME", UIFactory.Gray, ui.GoHome, 50).RT().Center(-165, -470, 310, 130);
        UIFactory.MakeButton(card, "SHELTER", UIFactory.Blue, ui.OpenCollection, 50).RT().Center(165, -470, 310, 130);
    }

    protected override void OnShow()
    {
        StageManager stage = StageManager.instance;
        bool cleared = stage.IsCleared;

        stageLabel.text = "STAGE " + stage.PlayedStage;
        title.text = cleared ? "MISSION COMPLETE!" : "TIME OUT!";
        title.color = cleared ? UIFactory.Green : UIFactory.Red;
        score.text = $"Rescued {stage.currentRescueCount} / {stage.targetRescueCount}";
        note.text = BuildNote(stage, cleared);
        note.color = stage.AdoptedNowCount > 0 ? UIFactory.Green : UIFactory.SubInk;
        retryLabel.text = cleared ? "NEXT STAGE" : "RETRY";

        BuildAnimalRow(stage);

        bool showFriends = stage.NewFriends.Count > 0;
        BuildFriends(stage.NewFriends);
        friendsSection.gameObject.SetActive(showFriends);
        hearts.Root.gameObject.SetActive(!showFriends);

        OnTick();
        ui.Run(UITween.PopIn(card));
    }

    protected override void OnTick()
    {
        hearts.Refresh();
        retryButton.interactable = HeartSystem.Hearts > 0;
    }

    static string BuildNote(StageManager stage, bool cleared)
    {
        // 입양 소식이 최우선
        var adopted = new List<string>();
        for (int i = 0; i < stage.Roster.Length; i++)
        {
            if (stage.WasAdoptedNow(i)) adopted.Add(AnimalCatalog.DisplayName(stage.Roster[i].name));
        }
        if (adopted.Count > 0)
        {
            return string.Join(", ", adopted) + (adopted.Count == 1 ? " found a new home!" : " found new homes!");
        }

        return cleared
            ? $"{Mathf.CeilToInt(stage.currentTime)}s left  -  Your heart is back!"
            : "Clear the stage to bring them to the shelter!";
    }

    void BuildAnimalRow(StageManager stage)
    {
        foreach (Transform child in animalRow) Object.Destroy(child.gameObject);

        Sprite[] roster = stage.Roster;
        if (roster == null || roster.Length == 0) return;

        float slotWidth = 820f / roster.Length;

        for (int i = 0; i < roster.Length; i++)
        {
            string id = roster[i].name;
            int count = stage.GetRescued(i);
            int total = CollectionData.GetCount(id);
            int goal = CollectionData.Goal(id);
            bool adopted = total >= goal;
            float x = -410f + slotWidth * (i + 0.5f);

            RectTransform slot = UIFactory.CreateRect("Slot" + i, animalRow).Center(x, 0, slotWidth, 240);

            bool saved = stage.IsCleared && count > 0;
            Image icon = UIFactory.Img(slot, "Icon", saved ? Color.white : new Color(1f, 1f, 1f, 0.35f), roster[i]);
            icon.preserveAspect = true;
            icon.rectTransform.Center(0, 45, 110, 110);

            UIFactory.Label(slot, "x " + count, 40, count > 0 ? UIFactory.Ink : UIFactory.Gray, true)
                .rectTransform.Center(0, -35, slotWidth, 48);

            // 입양까지 누적 진행도
            UIFactory.Bar bar = UIFactory.MakeBar(slot, "AdoptBar", adopted ? UIFactory.Green : UIFactory.Orange, 22);
            bar.Root.Center(0, -88, slotWidth - 30, 30);
            bar.Set((float)total / goal);
            bar.Text.text = adopted ? "HOME" : $"{total}/{goal}";

            if (stage.WasAdoptedNow(i))
            {
                AddBadge(slot, "ADOPTED", UIFactory.Green, 150);
            }
            else if (stage.IsNewDiscovery(i))
            {
                AddBadge(slot, "NEW", UIFactory.Orange, 96);
            }
        }
    }

    void AddBadge(RectTransform slot, string text, Color color, float width)
    {
        Image badge = UIFactory.Img(slot, "Badge", color, UIFactory.Rounded);
        badge.rectTransform.Center(0, 112, width, 42);
        UIFactory.Label(badge.transform, text, 24, Color.white, true).rectTransform.Stretch();
        ui.Run(UITween.PopIn(badge.transform, 0.4f));
    }

    void BuildFriends(IReadOnlyList<Sprite> friends)
    {
        foreach (Transform child in friendsSection) Object.Destroy(child.gameObject);
        if (friends.Count == 0) return;

        UIFactory.Label(friendsSection, friends.Count == 1 ? "A NEW FRIEND IS COMING!" : "NEW FRIENDS ARE COMING!",
            34, UIFactory.Blue, true).rectTransform.Center(0, 70, 820, 44);

        const float size = 96f, gap = 40f;
        float total = friends.Count * size + (friends.Count - 1) * gap;

        for (int i = 0; i < friends.Count; i++)
        {
            float x = -total / 2f + size / 2f + i * (size + gap);

            Image icon = UIFactory.Img(friendsSection, "Friend" + i, Color.white, friends[i]);
            icon.preserveAspect = true;
            icon.rectTransform.Center(x, -5, size, size);
            ui.Run(UITween.PopIn(icon.transform, 0.45f));

            UIFactory.Label(friendsSection, AnimalCatalog.DisplayName(friends[i].name), 26, UIFactory.Ink, true)
                .rectTransform.Center(x, -72, size + gap, 36);
        }
    }
}
