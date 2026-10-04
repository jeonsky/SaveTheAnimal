using TMPro;
using UnityEngine;

/// <summary>인게임 상단 HUD: 남은 시간 바, 구조 진행도 바, 일시정지 버튼</summary>
public class HUDView : UIView
{
    readonly UIFactory.Bar timeBar;
    readonly UIFactory.Bar rescueBar;
    int lastSecond = -1;

    StageManager Stage => StageManager.instance;

    public HUDView(RectTransform parent, UIManager ui) : base(parent, ui, "HUD")
    {
        var panel = UIFactory.Img(Root, "TopPanel", new Color(1f, 1f, 1f, 0.92f), UIFactory.Rounded);
        panel.rectTransform.TopStrip(40, 230, 30, 30);

        timeBar = BuildRow(panel.transform, "TIME", 32, UIFactory.Green);
        rescueBar = BuildRow(panel.transform, "RESCUE", 128, UIFactory.Orange);

        // 일시정지 버튼 (아이콘은 흰 막대 2개)
        var pauseBtn = UIFactory.MakeButton(panel.transform, "", UIFactory.Blue, ui.PauseGame);
        pauseBtn.RT().Place(new Vector2(1f, 0.5f), new Vector2(-35f, 0), new Vector2(160f, 160f));
        for (int i = 0; i < 2; i++)
        {
            var bar = UIFactory.Img(pauseBtn.transform, "PauseIcon", Color.white, UIFactory.Rounded);
            bar.rectTransform.Center(i == 0 ? -18f : 18f, 0, 22f, 68f);
        }
    }

    static UIFactory.Bar BuildRow(Transform parent, string label, float top, Color fillColor)
    {
        RectTransform row = UIFactory.CreateRect(label + "Row", parent).TopStrip(top, 72, 40, 225);

        TextMeshProUGUI title = UIFactory.Label(row, label, 36, UIFactory.SubInk, true, TextAlignmentOptions.Left);
        title.rectTransform.Place(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(190f, 72f));

        UIFactory.Bar bar = UIFactory.MakeBar(row, label + "Bar", fillColor, 38);
        bar.Root.Stretch(190, 8, 0, 8);
        return bar;
    }

    protected override void OnShow()
    {
        lastSecond = -1;
        if (Stage != null) SetRescue(Stage.currentRescueCount, Stage.targetRescueCount);
    }

    protected override void OnTick()
    {
        if (Stage == null) return;

        float time = Stage.currentTime;
        timeBar.Set(Stage.timeLimit > 0 ? time / Stage.timeLimit : 0f);
        timeBar.Fill.color = time > 20f ? UIFactory.Green : time > 10f ? UIFactory.Orange : UIFactory.Red;

        int sec = Mathf.CeilToInt(time);
        timeBar.Text.text = $"{sec / 60}:{sec % 60:00}";

        // 10초 이하부터 매 초 숫자가 쿵쿵 튐 → 긴장감
        if (sec != lastSecond)
        {
            if (lastSecond != -1 && sec <= 10 && sec > 0 && Stage.IsPlaying())
            {
                ui.Run(UITween.Punch(timeBar.Text.transform, 0.3f));
            }
            lastSecond = sec;
        }
    }

    public void SetRescue(int current, int target)
    {
        rescueBar.Set(target > 0 ? (float)current / target : 0f);
        rescueBar.Text.text = $"{Mathf.Min(current, target)} / {target}";

        if (current > 0 && IsVisible)
        {
            ui.Run(UITween.Punch(rescueBar.Text.transform, 0.25f));
        }
    }
}
