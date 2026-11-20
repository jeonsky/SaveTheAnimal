using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보호소 (구조한 동물 도감 & 입양 보내기).
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
        public Button AdoptButton;
    }

    static readonly Color AdoptedTint = UIFactory.Hex("E6F6EA");
    static readonly Color ReadyTint   = UIFactory.Hex("FFF2DC");

    readonly RectTransform card;
    readonly RectTransform content;
    readonly ScrollRect scroll;
    readonly TextMeshProUGUI progress;
    readonly List<Cell> cells = new List<Cell>();
    IReadOnlyList<Sprite> animals;

    // 입양 축하 팝업 UI
    readonly Image popupDim;
    readonly RectTransform popupCard;
    readonly Image popupIcon;
    readonly TextMeshProUGUI popupTitle;
    readonly TextMeshProUGUI popupDesc;

    public CollectionView(RectTransform parent, UIManager ui) : base(parent, ui, "Collection")
    {
        AddBlocker(UIFactory.Dim);

        card = UIFactory.Img(Root, "Card", UIFactory.Cream, UIFactory.Rounded).rectTransform.Stretch(40, 120, 40, 120);

        UIFactory.Label(card, "SHELTER", 84, UIFactory.Orange, true).rectTransform.TopStrip(36, 110);

        progress = UIFactory.Label(card, "", 40, UIFactory.SubInk, true);
        progress.rectTransform.TopStrip(146, 56);

        // 스크롤 영역
        Image scrollArea = UIFactory.Img(card, "Scroll", new Color(0, 0, 0, 0));
        scrollArea.raycastTarget = true;
        scrollArea.rectTransform.Stretch(30, 220, 30, 220);
        scrollArea.gameObject.AddComponent<RectMask2D>();

        content = UIFactory.CreateRect("Content", scrollArea.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200, 315);
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

        // ─── 입양 축하 미니 팝업 ───
        popupDim = UIFactory.Img(Root, "AdoptPopupDim", UIFactory.Dim);
        popupDim.raycastTarget = true;
        popupDim.rectTransform.Stretch();

        popupCard = UIFactory.Img(popupDim.transform, "AdoptPopupCard", UIFactory.Cream, UIFactory.Rounded)
            .rectTransform.Center(0, 0, 780, 760);

        popupTitle = UIFactory.Label(popupCard, "HAPPY ADOPTION!", 64, UIFactory.Green, true);
        popupTitle.rectTransform.Center(0, 270, 720, 90);

        popupIcon = UIFactory.Img(popupCard, "AdoptIcon", Color.white);
        popupIcon.preserveAspect = true;
        popupIcon.rectTransform.Center(0, 70, 220, 220);

        popupDesc = UIFactory.Label(popupCard, "", 38, UIFactory.Ink, true);
        popupDesc.textWrappingMode = TextWrappingModes.Normal;
        popupDesc.rectTransform.Center(0, -110, 680, 120);

        var okBtn = UIFactory.MakeButton(popupCard, "AWESOME!", UIFactory.Green, () =>
        {
            popupDim.gameObject.SetActive(false);
        }, 52);
        okBtn.RT().Center(0, -260, 460, 130);

        popupDim.gameObject.SetActive(false);
    }

    protected override void OnShow()
    {
        popupDim.gameObject.SetActive(false);
        BuildCells();
        Refresh();
        scroll.verticalNormalizedPosition = 1f;
        ui.Run(UITween.PopIn(card));
    }

    void BuildCells()
    {
        animals = AnimalCatalog.All;
        if (cells.Count == animals.Count) return;

        foreach (Transform child in content) Object.Destroy(child.gameObject);
        cells.Clear();

        for (int i = 0; i < animals.Count; i++)
        {
            int index = i;
            Sprite sprite = animals[i];
            var cell = new Cell();

            cell.Background = UIFactory.Img(content, "Cell", Color.white, UIFactory.Rounded);
            Transform bg = cell.Background.transform;

            cell.Icon = UIFactory.Img(bg, "Icon", Color.white, sprite);
            cell.Icon.preserveAspect = true;
            cell.Icon.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(115, 115));

            cell.Name = UIFactory.Label(bg, "", 30, UIFactory.Ink, true);
            cell.Name.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -138), new Vector2(186, 40));
            cell.Name.enableAutoSizing = true;
            cell.Name.fontSizeMin = 20;
            cell.Name.fontSizeMax = 30;

            cell.Bar = UIFactory.MakeBar(bg, "AdoptBar", UIFactory.Orange, 20);
            cell.Bar.Root.Place(new Vector2(0.5f, 1f), new Vector2(0, -186), new Vector2(170, 28));

            cell.Status = UIFactory.Label(bg, "", 25, UIFactory.SubInk, true);
            cell.Status.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -232), new Vector2(186, 44));

            cell.AdoptButton = UIFactory.MakeButton(bg, "ADOPT!", UIFactory.Green, () => OnClickAdopt(index), 26);
            cell.AdoptButton.RT().Place(new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(168, 56));

            cells.Add(cell);
        }
    }

    void OnClickAdopt(int index)
    {
        Sprite sprite = animals[index];
        string id = sprite.name;

        if (!CollectionData.Adopt(id)) return;

        ui.OnAnimalAdopted();
        Refresh();

        popupIcon.sprite = sprite;
        popupDesc.text = $"{AnimalCatalog.DisplayName(id)} is now playing in the Yard!\n(+1 Heart Reward!)";
        popupDim.gameObject.SetActive(true);
        ui.Run(UITween.PopIn(popupCard));
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
                cell.Background.color = Color.white;
                cell.Icon.color = new Color(0f, 0f, 0f, 0.75f);
                cell.Name.text = "???";
                cell.Bar.Root.gameObject.SetActive(false);
                cell.Status.gameObject.SetActive(true);
                cell.Status.text = "Not met yet";
                cell.Status.color = UIFactory.Gray;
                cell.AdoptButton.gameObject.SetActive(false);
                continue;
            }

            bool adopted = CollectionData.IsAdopted(id);
            bool canAdopt = CollectionData.CanAdopt(id);
            if (adopted) adoptedCount++;

            cell.Background.color = adopted ? AdoptedTint : canAdopt ? ReadyTint : Color.white;
            cell.Icon.color = Color.white;
            cell.Name.text = AnimalCatalog.DisplayName(id);

            cell.Bar.Root.gameObject.SetActive(true);
            cell.Bar.Fill.color = adopted ? UIFactory.Green : UIFactory.Orange;
            cell.Bar.Set((float)count / goal);
            cell.Bar.Text.text = (adopted || canAdopt) ? $"{goal}/{goal}" : $"{count}/{goal}";

            if (canAdopt)
            {
                cell.Status.gameObject.SetActive(false);
                cell.AdoptButton.gameObject.SetActive(true);
            }
            else
            {
                cell.AdoptButton.gameObject.SetActive(false);
                cell.Status.gameObject.SetActive(true);
                cell.Status.text = adopted ? "ADOPTED" : "In our care";
                cell.Status.color = adopted ? UIFactory.Green : UIFactory.Orange;
            }
        }

        progress.text = $"Adopted {adoptedCount} / {cells.Count}";
    }
}
