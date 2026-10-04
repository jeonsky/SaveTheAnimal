using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>하트 아이콘 줄 + 다음 하트까지 남은 시간 표시</summary>
public class HeartsWidget
{
    public RectTransform Root { get; }

    readonly Image[] hearts;
    readonly TextMeshProUGUI info;

    public HeartsWidget(Transform parent, float iconSize = 84f)
    {
        Root = UIFactory.CreateRect("Hearts", parent);

        int max = HeartSystem.MaxHearts;
        hearts = new Image[max];

        const float gap = 14f;
        float total = max * iconSize + (max - 1) * gap;

        for (int i = 0; i < max; i++)
        {
            Image h = UIFactory.Img(Root, "Heart" + i, UIFactory.Red, UIFactory.Heart);
            float x = -total / 2f + iconSize / 2f + i * (iconSize + gap);
            h.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(x, 0), new Vector2(iconSize, iconSize));
            hearts[i] = h;
        }

        info = UIFactory.Label(Root, "", 36, UIFactory.SubInk);
        info.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -iconSize - 8f), new Vector2(total + 300f, 50f));
    }

    public void Refresh()
    {
        int count = HeartSystem.Hearts;

        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].color = i < count ? UIFactory.Red : UIFactory.Track;
        }

        if (count >= HeartSystem.MaxHearts)
        {
            info.text = "Hearts full!";
        }
        else
        {
            int s = HeartSystem.SecondsToNextHeart;
            info.text = $"Next heart in {s / 60:00}:{s % 60:00}";
        }
    }
}
