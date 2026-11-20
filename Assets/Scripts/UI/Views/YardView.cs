using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 홈에서 YARD 버튼을 누르면 열리는 전용 마당 화면.
/// - 오른쪽 위 언덕에 빨간 지붕 집(Resources/yard_house)을 배치하고,
///   집의 빨간 현관문 앞 잔디 마당으로 동물들이 드나들며 뛰어논다.
/// </summary>
public class YardView : UIView
{
    enum PetState { Idle, Walking, EnteringHouse, InsideHouse, ExitingHouse }

    class YardPet
    {
        public RectTransform Root;
        public Image Icon;
        public CanvasGroup Group;
        public PetState State;
        public Vector2 TargetPos;
        public float Timer;
        public float Speed;
        public float BobPhase;
        public float Facing = 1f;
    }

    static readonly Color SkyColor    = UIFactory.Hex("D8F1FF");
    static readonly Color GrassColor  = UIFactory.Hex("CBEAC0");
    static readonly Color GrassDeep   = UIFactory.Hex("B5DBA8");
    static readonly Color FenceColor  = UIFactory.Hex("E8C79B");
    static readonly Color HouseWall   = UIFactory.Hex("FFF3E0");
    static readonly Color HouseRoof   = UIFactory.Hex("E87A5D");
    static readonly Color HouseDoor   = UIFactory.Hex("5C4033");

    readonly RectTransform card;
    readonly RectTransform yardArea;
    readonly RectTransform petLayer;
    readonly TextMeshProUGUI countLabel;
    readonly TextMeshProUGUI emptyHint;
    readonly List<YardPet> pets = new List<YardPet>();
    readonly HashSet<string> spawnedIds = new HashSet<string>();

    // 마당 활동 범위 & 오른쪽 집 빨간 현관문 앞 정확 좌표
    readonly Vector2 yardMin = new Vector2(-360f, -450f);
    readonly Vector2 yardMax = new Vector2(360f, 80f);
    Vector2 doorPos = new Vector2(130f, 115f);

    public YardView(RectTransform parent, UIManager ui) : base(parent, ui, "Yard")
    {
        AddBlocker(UIFactory.Dim);

        card = UIFactory.Img(Root, "Card", UIFactory.Cream, UIFactory.Rounded).rectTransform.Stretch(40, 110, 40, 110);

        UIFactory.Label(card, "HAPPY YARD", 80, UIFactory.Orange, true).rectTransform.TopStrip(32, 100);

        countLabel = UIFactory.Label(card, "", 38, UIFactory.SubInk, true);
        countLabel.rectTransform.TopStrip(132, 50);

        // 쉘터 도감 폭과 딱 맞는 920px 폭의 아늑한 마당 액자
        Image yardFrame = UIFactory.Img(card, "YardFrame", GrassDeep, UIFactory.Rounded);
        yardArea = yardFrame.rectTransform;
        yardArea.anchorMin = new Vector2(0.5f, 0f);
        yardArea.anchorMax = new Vector2(0.5f, 1f);
        yardArea.pivot = new Vector2(0.5f, 0.5f);
        yardArea.sizeDelta = new Vector2(920f, 0f);
        yardArea.offsetMin = new Vector2(-460f, 210f);
        yardArea.offsetMax = new Vector2(460f, -195f);
        yardFrame.gameObject.AddComponent<RectMask2D>();

        BuildScenery(yardArea);

        // 동물들이 돌아다니는 레이어 (집보다 앞에 그려져서 문 앞 출입이 자연스러움)
        petLayer = UIFactory.CreateRect("PetLayer", yardArea).Stretch();

        emptyHint = UIFactory.Label(yardArea, "No adopted friends yet!\nRescue animals & adopt them in SHELTER.", 38, UIFactory.SubInk, true);
        emptyHint.rectTransform.Center(0, -150, 760, 140);
        emptyHint.textWrappingMode = TextWrappingModes.Normal;

        var close = UIFactory.MakeButton(card, "CLOSE", UIFactory.Gray, ui.CloseYard, 54);
        close.RT().Place(new Vector2(0.5f, 0f), new Vector2(0, 48), new Vector2(520, 135));
    }

    void BuildScenery(Transform parent)
    {
        // 1. 위쪽 맑은 하늘 영역
        Image sky = UIFactory.Img(parent, "Sky", SkyColor);
        sky.rectTransform.TopStrip(0, 360);

        // 왼쪽 하늘 위 뭉게구름
        CreateCloud(sky.transform, new Vector2(-240, -85), 1.0f);
        CreateCloud(sky.transform, new Vector2(-95, -145), 0.72f);

        // 2. 아래쪽 넓은 잔디 마당
        Image grass = UIFactory.Img(parent, "Grass", GrassColor, UIFactory.Rounded);
        grass.rectTransform.Stretch(0, 0, 0, 290);

        // 잔디 포인트 & 작은 꽃들
        CreateGrassTuft(grass.transform, new Vector2(-240, -120));
        CreateGrassTuft(grass.transform, new Vector2(180, -160));
        CreateGrassTuft(grass.transform, new Vector2(260, -320));
        CreateGrassTuft(grass.transform, new Vector2(-80, -350));
        CreateFlower(grass.transform, new Vector2(-260, 30), UIFactory.Red);
        CreateFlower(grass.transform, new Vector2(-140, -260), UIFactory.Orange);
        CreateFlower(grass.transform, new Vector2(90, -390), Color.white);

        // 3. 왼쪽 울타리 (오른쪽 집과 나란히 이어짐)
        for (int i = 0; i < 5; i++)
        {
            float x = -375f + i * 74f;
            Image post = UIFactory.Img(parent, "FencePost" + i, FenceColor, UIFactory.Rounded);
            post.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(x, -265), new Vector2(24, 68));
        }
        Image rail = UIFactory.Img(parent, "FenceRail", FenceColor, UIFactory.Rounded);
        rail.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(-227, -286), new Vector2(345, 16));

        // 4. 오른쪽에 귀여운 빨간 지붕 집 일러스트 배치 (Resources/yard_house)
        Sprite houseSprite = Resources.Load<Sprite>("yard_house");
        if (houseSprite != null)
        {
            Image houseImg = UIFactory.Img(parent, "HouseSprite", Color.white, houseSprite);
            houseImg.preserveAspect = true;
            // 오른쪽(x = +195)에 배치 -> 이미지 속 빨간 현관문이 마당 안쪽(중앙 방향)을 향함!
            houseImg.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(195, -25), new Vector2(470, 365));
            doorPos = new Vector2(130f, 115f); // 이미지의 빨간 현관문 앞 좌표
        }
        else
        {
            BuildFallbackHouse(parent);
        }
    }

    void BuildFallbackHouse(Transform parent)
    {
        RectTransform house = UIFactory.CreateRect("CornerHouse", parent);
        house.Place(new Vector2(0.5f, 1f), new Vector2(215, -135), new Vector2(270, 235));

        Image wall = UIFactory.Img(house, "Wall", HouseWall, UIFactory.Rounded);
        wall.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(230, 158));

        Image roof = UIFactory.Img(house, "Roof", HouseRoof, UIFactory.Rounded);
        roof.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(264, 74));

        Image door = UIFactory.Img(wall.transform, "Door", HouseDoor, UIFactory.Rounded);
        door.rectTransform.Place(new Vector2(0.45f, 0f), new Vector2(0, 0), new Vector2(66, 98));
        doorPos = new Vector2(205f, 135f);
    }

    static void CreateCloud(Transform parent, Vector2 pos, float scale)
    {
        RectTransform c = UIFactory.CreateRect("Cloud", parent);
        c.Place(new Vector2(0.5f, 1f), pos, new Vector2(160 * scale, 54 * scale));
        UIFactory.Img(c, "Base", Color.white, UIFactory.Rounded).rectTransform.Stretch(0, 0, 0, 14 * scale);
        UIFactory.Img(c, "Top", Color.white, UIFactory.Rounded).rectTransform.Center(-14 * scale, 10 * scale, 84 * scale, 44 * scale);
    }

    static void CreateGrassTuft(Transform parent, Vector2 pos)
    {
        Image tuft = UIFactory.Img(parent, "Tuft", GrassDeep, UIFactory.Rounded);
        tuft.rectTransform.Center(pos.x, pos.y, 52, 18);
    }

    static void CreateFlower(Transform parent, Vector2 pos, Color color)
    {
        Image f = UIFactory.Img(parent, "Flower", color, UIFactory.Rounded);
        f.rectTransform.Center(pos.x, pos.y, 20, 20);
    }

    protected override void OnShow()
    {
        SyncAdoptedAnimals();
        ui.Run(UITween.PopIn(card));
    }

    protected override void OnTick()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        for (int i = 0; i < pets.Count; i++)
        {
            UpdatePet(pets[i], dt);
        }
    }

    void SyncAdoptedAnimals()
    {
        IReadOnlyList<Sprite> all = AnimalCatalog.All;
        for (int i = 0; i < all.Count; i++)
        {
            Sprite s = all[i];
            if (CollectionData.IsAdopted(s.name) && !spawnedIds.Contains(s.name))
            {
                SpawnPet(s);
                spawnedIds.Add(s.name);
            }
        }

        emptyHint.gameObject.SetActive(pets.Count == 0);
        countLabel.text = pets.Count > 0
            ? $"{pets.Count} happy friends playing outside!"
            : "Adopt animals in Shelter to invite them here!";
    }

    void SpawnPet(Sprite sprite)
    {
        RectTransform rt = UIFactory.CreateRect("YardPet_" + sprite.name, petLayer);
        Vector2 startPos = new Vector2(Random.Range(-280f, 160f), Random.Range(yardMin.y, yardMax.y));
        rt.Center(startPos.x, startPos.y, 118, 118);

        var cg = rt.gameObject.AddComponent<CanvasGroup>();

        Image icon = UIFactory.Img(rt, "Icon", Color.white, sprite);
        icon.preserveAspect = true;
        icon.raycastTarget = true;
        icon.rectTransform.Stretch();

        var pet = new YardPet
        {
            Root = rt,
            Icon = icon,
            Group = cg,
            State = PetState.Idle,
            Timer = Random.Range(0.3f, 1.5f),
            Speed = Random.Range(75f, 120f),
            BobPhase = Random.Range(0f, Mathf.PI * 2f),
        };

        var btn = icon.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => OnPetTapped(pet));

        pets.Add(pet);
    }

    void OnPetTapped(YardPet pet)
    {
        if (pet.State == PetState.InsideHouse || pet.State == PetState.EnteringHouse) return;

        ui.Run(UITween.Punch(pet.Icon.transform, 0.35f, 0.25f));

        Image heart = UIFactory.Img(petLayer, "LoveHeart", UIFactory.Red, UIFactory.Heart);
        Vector2 pos = pet.Root.anchoredPosition + new Vector2(0, 72f);
        heart.rectTransform.Center(pos.x, pos.y, 42, 42);
        ui.Run(FloatHeartRoutine(heart));
    }

    static System.Collections.IEnumerator FloatHeartRoutine(Image heart)
    {
        RectTransform rt = heart.rectTransform;
        Vector2 start = rt.anchoredPosition;
        const float duration = 0.7f;

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            if (heart == null) yield break;
            float p = t / duration;
            rt.anchoredPosition = start + new Vector2(0, 60f * p);
            Color c = heart.color;
            c.a = 1f - p;
            heart.color = c;
            yield return null;
        }

        if (heart != null) Object.Destroy(heart.gameObject);
    }

    void UpdatePet(YardPet pet, float dt)
    {
        switch (pet.State)
        {
            case PetState.Idle:
                pet.Icon.rectTransform.anchoredPosition = Vector2.zero;
                pet.Icon.rectTransform.localRotation = Quaternion.identity;
                pet.Timer -= dt;
                if (pet.Timer <= 0f)
                {
                    if (Random.value < 0.25f)
                    {
                        pet.State = PetState.EnteringHouse;
                        pet.TargetPos = doorPos;
                    }
                    else
                    {
                        pet.State = PetState.Walking;
                        pet.TargetPos = new Vector2(
                            Random.Range(yardMin.x, yardMax.x),
                            Random.Range(yardMin.y, yardMax.y)
                        );
                    }
                }
                break;

            case PetState.Walking:
            case PetState.EnteringHouse:
                MoveTowardsTarget(pet, dt);
                if (Vector2.Distance(pet.Root.anchoredPosition, pet.TargetPos) < 8f)
                {
                    if (pet.State == PetState.EnteringHouse)
                    {
                        pet.State = PetState.InsideHouse;
                        pet.Timer = Random.Range(2.0f, 4.5f);
                        pet.Group.alpha = 0f;
                        pet.Icon.raycastTarget = false;
                    }
                    else
                    {
                        pet.State = PetState.Idle;
                        pet.Timer = Random.Range(1.2f, 3.0f);
                    }
                }
                break;

            case PetState.InsideHouse:
                pet.Timer -= dt;
                if (pet.Timer <= 0f)
                {
                    pet.Root.anchoredPosition = doorPos;
                    pet.Group.alpha = 1f;
                    pet.Icon.raycastTarget = true;
                    pet.State = PetState.ExitingHouse;
                    pet.TargetPos = new Vector2(Random.Range(-240f, 180f), Random.Range(yardMin.y, 20f));
                    ui.Run(UITween.PopIn(pet.Icon.transform, 0.3f));
                }
                break;

            case PetState.ExitingHouse:
                MoveTowardsTarget(pet, dt);
                if (Vector2.Distance(pet.Root.anchoredPosition, pet.TargetPos) < 8f)
                {
                    pet.State = PetState.Idle;
                    pet.Timer = Random.Range(1.0f, 2.5f);
                }
                break;
        }
    }

    void MoveTowardsTarget(YardPet pet, float dt)
    {
        Vector2 current = pet.Root.anchoredPosition;
        Vector2 next = Vector2.MoveTowards(current, pet.TargetPos, pet.Speed * dt);
        pet.Root.anchoredPosition = next;

        float dx = pet.TargetPos.x - current.x;
        if (Mathf.Abs(dx) > 1f)
        {
            pet.Facing = dx >= 0f ? 1f : -1f;
            pet.Icon.rectTransform.localScale = new Vector3(pet.Facing, 1f, 1f);
        }

        pet.BobPhase += dt * 13f;
        float bounce = Mathf.Abs(Mathf.Sin(pet.BobPhase)) * 11f;
        float tilt = Mathf.Sin(pet.BobPhase) * 6f;
        pet.Icon.rectTransform.anchoredPosition = new Vector2(0, bounce);
        pet.Icon.rectTransform.localRotation = Quaternion.Euler(0, 0, tilt);
    }
}
