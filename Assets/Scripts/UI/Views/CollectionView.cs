using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>동물 도감: 동물별 누적 구조 수, 레벨, 다음 레벨까지 진행도. 미발견 동물은 실루엣.</summary>
public class CollectionView : UIView
{
    class Cell
    {
        public Image Icon;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Level;
        public TextMeshProUGUI Count;
        public UIFactory.Bar Bar;
    }

    readonly RectTransform card;
    readonly RectTransform grid;
    readonly TextMeshProUGUI progress;
    readonly List<Cell> cells = new List<Cell>();
    Sprite[] sprites;

    public CollectionView(RectTransform parent, UIManager ui) : base(parent, ui, "Collection")
    {
        AddBlocker(UIFactory.Dim);

        card = UIFactory.Img(Root, "Card", UIFactory.Cream, UIFactory.Rounded).rectTransform.Stretch(50, 150, 50, 150);

        UIFactory.Label(card, "COLLECTION", 84, UIFactory.Orange, true).rectTransform.TopStrip(40, 110);

        progress = UIFactory.Label(card, "", 40, UIFactory.SubInk);
        progress.rectTransform.TopStrip(150, 60);

        grid = UIFactory.CreateRect("Grid", card).TopStrip(240, 900, 40, 40);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(280, 420);
        layout.spacing = new Vector2(24, 28);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.childAlignment = TextAnchor.UpperCenter;

        var close = UIFactory.MakeButton(card, "CLOSE", UIFactory.Gray, ui.CloseCollection, 54);
        close.RT().Place(new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(520, 140));
    }

    protected override void OnShow()
    {
        BuildOnce();
        Refresh();
        ui.Run(UITween.PopIn(card));
    }

    void BuildOnce()
    {
        if (cells.Count > 0) return;

        sprites = BoardManager.instance != null ? BoardManager.instance.animalSprites : null;
        if (sprites == null) return;

        foreach (Sprite sprite in sprites)
        {
            Image bg = UIFactory.Img(grid, "Cell", Color.white, UIFactory.Rounded);

            var cell = new Cell();

            cell.Icon = UIFactory.Img(bg.transform, "Icon", Color.white, sprite);
            cell.Icon.preserveAspect = true;
            cell.Icon.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(180, 180));

            cell.Name = UIFactory.Label(bg.transform, "", 42, UIFactory.Ink, true);
            cell.Name.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -212), new Vector2(260, 56));
            cell.Name.enableAutoSizing = true;
            cell.Name.fontSizeMin = 26;
            cell.Name.fontSizeMax = 42;

            cell.Level = UIFactory.Label(bg.transform, "", 34, UIFactory.Orange, true);
            cell.Level.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -266), new Vector2(260, 46));

            cell.Count = UIFactory.Label(bg.transform, "", 30, UIFactory.SubInk);
            cell.Count.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -310), new Vector2(260, 42));

            cell.Bar = UIFactory.MakeBar(bg.transform, "LevelBar", UIFactory.Green, 26);
            cell.Bar.Root.Place(new Vector2(0.5f, 1f), new Vector2(0, -362), new Vector2(230, 40));

            cells.Add(cell);
        }
    }

    void Refresh()
    {
        if (sprites == null) return;

        int found = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            Cell cell = cells[i];
            string id = sprites[i].name;
            int count = CollectionData.GetCount(id);

            if (count <= 0)
            {
                // 미발견: 검은 실루엣
                cell.Icon.color = new Color(0f, 0f, 0f, 0.75f);
                cell.Name.text = "???";
                cell.Level.text = "NOT FOUND";
                cell.Level.color = UIFactory.Gray;
                cell.Count.text = "Rescue to discover";
                cell.Bar.Set(0f);
                cell.Bar.Text.text = "";
                continue;
            }

            found++;
            int level = CollectionData.GetLevel(count);
            int next = CollectionData.NextThreshold(count);
            int current = CollectionData.CurrentThreshold(count);

            cell.Icon.color = Color.white;
            cell.Name.text = CollectionData.DisplayName(id);
            cell.Level.text = level >= CollectionData.MaxLevel ? "Lv.MAX" : "Lv." + level;
            cell.Level.color = UIFactory.Orange;
            cell.Count.text = "Rescued " + count;

            if (next < 0)
            {
                cell.Bar.Set(1f);
                cell.Bar.Text.text = "MAX";
            }
            else
            {
                cell.Bar.Set((float)(count - current) / (next - current));
                cell.Bar.Text.text = $"{count}/{next}";
            }
        }

        progress.text = $"Discovered {found} / {cells.Count}";
    }
}
