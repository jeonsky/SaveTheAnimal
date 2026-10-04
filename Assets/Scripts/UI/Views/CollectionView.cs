using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보호소 (구조한 동물 도감, 스크롤).
/// - 아직 못 만난 동물: 검은 실루엣 + ???
/// - 돌보는 중: 입양까지 누적 구조 진행도
/// - 입양 완료: 초록 카드 + ADOPTED
/// </summary>
public class CollectionView : UIView
{
    class Cell
    {
        public Image Background;
        public Image Icon;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Status;
        public UIFactory.Bar Bar;
    }

    static readonly Color AdoptedTint = UIFactory.Hex("E6F6EA");

    readonly RectTransform card;
    readonly RectTransform content;
    readonly ScrollRect scroll;
    readonly TextMeshProUGUI progress;
    readonly List<Cell> cells = new List<Cell>();
    IReadOnlyList<Sprite> animals;

    public CollectionView(RectTransform parent, UIManager ui) : base(parent, ui, "Collection")
    {
        AddBlocker(UIFactory.Dim);

        card = UIFactory.Img(Root, "Card", UIFactory.Cream, UIFactory.Rounded).rectTransform.Stretch(40, 120, 40, 120);

        UIFactory.Label(card, "SHELTER", 84, UIFactory.Orange, true).rectTransform.TopStrip(36, 110);

        progress = UIFactory.Label(card, "", 40, UIFactory.SubInk, true);
        progress.rectTransform.TopStrip(146, 56);

        // 스크롤 영역 (뷰포트 = 자기 자신, RectMask2D로 바깥 잘라냄)
        Image scrollArea = UIFactory.Img(card, "Scroll", new Color(0, 0, 0, 0));
        scrollArea.raycastTarget = true; // 드래그 받기
        scrollArea.rectTransform.Stretch(30, 220, 30, 220);
        scrollArea.gameObject.AddComponent<RectMask2D>();

        content = UIFactory.CreateRect("Content", scrollArea.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200, 300);
        grid.spacing = new Vector2(18, 20);
        grid.padding = new RectOffset(0, 0, 10, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = scrollArea.rectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 40f;

        var close = UIFactory.MakeButton(card, "CLOSE", UIFactory.Gray, ui.CloseCollection, 54);
        close.RT().Place(new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(520, 140));
    }

    protected override void OnShow()
    {
        BuildCells();
        Refresh();
        scroll.verticalNormalizedPosition = 1f; // 맨 위부터
        ui.Run(UITween.PopIn(card));
    }

    void BuildCells()
    {
        animals = AnimalCatalog.All;
        if (cells.Count == animals.Count) return;

        foreach (Transform child in content) Object.Destroy(child.gameObject);
        cells.Clear();

        foreach (Sprite sprite in animals)
        {
            var cell = new Cell();

            cell.Background = UIFactory.Img(content, "Cell", Color.white, UIFactory.Rounded);
            Transform bg = cell.Background.transform;

            cell.Icon = UIFactory.Img(bg, "Icon", Color.white, sprite);
            cell.Icon.preserveAspect = true;
            cell.Icon.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(120, 120));

            cell.Name = UIFactory.Label(bg, "", 32, UIFactory.Ink, true);
            cell.Name.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -146), new Vector2(186, 42));
            cell.Name.enableAutoSizing = true;
            cell.Name.fontSizeMin = 20;
            cell.Name.fontSizeMax = 32;

            cell.Bar = UIFactory.MakeBar(bg, "AdoptBar", UIFactory.Orange, 22);
            cell.Bar.Root.Place(new Vector2(0.5f, 1f), new Vector2(0, -198), new Vector2(170, 30));

            cell.Status = UIFactory.Label(bg, "", 26, UIFactory.SubInk, true);
            cell.Status.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -240), new Vector2(186, 38));

            cells.Add(cell);
        }
    }

    void Refresh()
    {
        int adoptedCount = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            Cell cell = cells[i];
            string id = animals[i].name;
            int count = CollectionData.GetCount(id);
            int goal = CollectionData.Goal(id);

            if (count <= 0)
            {
                // 아직 못 만난 동물
                cell.Background.color = Color.white;
                cell.Icon.color = new Color(0f, 0f, 0f, 0.75f);
                cell.Name.text = "???";
                cell.Bar.Root.gameObject.SetActive(false);
                cell.Status.text = "Not met yet";
                cell.Status.color = UIFactory.Gray;
                continue;
            }

            bool adopted = count >= goal;
            if (adopted) adoptedCount++;

            cell.Background.color = adopted ? AdoptedTint : Color.white;
            cell.Icon.color = Color.white;
            cell.Name.text = AnimalCatalog.DisplayName(id);

            cell.Bar.Root.gameObject.SetActive(true);
            cell.Bar.Fill.color = adopted ? UIFactory.Green : UIFactory.Orange;
            cell.Bar.Set((float)count / goal);
            cell.Bar.Text.text = adopted ? $"{goal}/{goal}" : $"{count}/{goal}";

            cell.Status.text = adopted ? "ADOPTED" : "In our care";
            cell.Status.color = adopted ? UIFactory.Green : UIFactory.Orange;
        }

        progress.text = $"Adopted {adoptedCount} / {cells.Count}";
    }
}
