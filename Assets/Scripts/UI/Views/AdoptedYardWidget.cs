using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 입양 완료된 동물들이 뛰어노는 아늑한 마당 위젯.
/// - 구석에 작은 집과 문이 있어 동물들이 가끔 집 안으로 들어갔다 나온다.
/// - 동물들은 마당을 아장아장 돌아다니며, 터치하면 점프하면서 하트를 띄운다.
/// </summary>
public class AdoptedYardWidget
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

    static readonly Color GrassColor  = UIFactory.Hex("DFF2D8");
    static readonly Color GrassBorder = UIFactory.Hex("CBE6C1");
    static readonly Color HouseWall   = UIFactory.Hex("FFF1DC");
    static readonly Color HouseRoof   = UIFactory.Hex("E87A5D");
    static readonly Color HouseDoor   = UIFactory.Hex("5C4033");

    public RectTransform Root { get; }

    readonly UIManager ui;
    readonly RectTransform petLayer;
    readonly TextMeshProUGUI emptyHint;
    readonly List<YardPet> pets = new List<YardPet>();
    readonly HashSet<string> spawnedIds = new HashSet<string>();

    // 마당 이동 가능 범위 & 집 문 위치
    readonly Vector2 yardMin = new Vector2(-380f, -135f);
    readonly Vector2 yardMax = new Vector2(390f, 95f);
    readonly Vector2 doorPos = new Vector2(-335f, 45f);

    public AdoptedYardWidget(Transform parent, UIManager ui)
    {
        this.ui = ui;

        Image border = UIFactory.Img(parent, "AdoptedYard", GrassBorder, UIFactory.Rounded);
        Root = border.rectTransform;

        Image grass = UIFactory.Img(Root, "Grass", GrassColor, UIFactory.Rounded);
        grass.rectTransform.Stretch(8, 8, 8, 8);
        grass.gameObject.AddComponent<RectMask2D>();

        // 마당 잔디 포인트 디테일
        CreateGrassTuft(grass.transform, new Vector2(-120, -90));
        CreateGrassTuft(grass.transform, new Vector2(180, 60));
        CreateGrassTuft(grass.transform, new Vector2(270, -100));
        CreateGrassTuft(grass.transform, new Vector2(20, -20));

        // ─── 구석의 아늑한 집 (지붕 + 벽 + 문) ───
        BuildCornerHouse(grass.transform);

        // 동물들이 돌아다니는 레이어
        petLayer = UIFactory.CreateRect("PetLayer", grass.transform).Stretch();

        emptyHint = UIFactory.Label(grass.transform, "Adopt animals below to see them play in the yard!", 30, UIFactory.SubInk, true);
        emptyHint.rectTransform.Center(70, -20, 680, 80);
        emptyHint.textWrappingMode = TextWrappingModes.Normal;
    }

    void BuildCornerHouse(Transform parent)
    {
        RectTransform house = UIFactory.CreateRect("CornerHouse", parent);
        house.Place(new Vector2(0f, 1f), new Vector2(26, -18), new Vector2(210, 170));

        // 집 벽
        Image wall = UIFactory.Img(house, "Wall", HouseWall, UIFactory.Rounded);
        wall.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(176, 118));

        // 지붕
        Image roof = UIFactory.Img(house, "Roof", HouseRoof, UIFactory.Rounded);
        roof.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(204, 58));

        // 드나들 수 있는 현관문
        Image door = UIFactory.Img(wall.transform, "Door", HouseDoor, UIFactory.Rounded);
        door.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(54, 76));

        // 작은 창문
        Image win = UIFactory.Img(wall.transform, "Window", UIFactory.Hex("A8D8EA"), UIFactory.Rounded);
        win.rectTransform.Place(new Vector2(0.18f, 0.58f), Vector2.zero, new Vector2(30, 30));
    }

    static void CreateGrassTuft(Transform parent, Vector2 pos)
    {
        Image tuft = UIFactory.Img(parent, "Tuft", UIFactory.Hex("C5E3BA"), UIFactory.Rounded);
        tuft.rectTransform.Center(pos.x, pos.y, 44, 16);
    }

    /// <summary>입양 완료 목록을 확인해 새로 입양된 동물이 있으면 마당에 추가한다.</summary>
    public void SyncAdoptedAnimals()
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
    }

    void SpawnPet(Sprite sprite)
    {
        RectTransform rt = UIFactory.CreateRect("YardPet_" + sprite.name, petLayer);
        Vector2 startPos = new Vector2(Random.Range(-180f, 320f), Random.Range(yardMin.y, yardMax.y));
        rt.Center(startPos.x, startPos.y, 92, 92);

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
            Timer = Random.Range(0.4f, 1.8f),
            Speed = Random.Range(65f, 105f),
            BobPhase = Random.Range(0f, Mathf.PI * 2f),
        };

        // 터치 시 점프 + 하트 연출
        var btn = icon.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => OnPetTapped(pet));

        pets.Add(pet);
    }

    void OnPetTapped(YardPet pet)
    {
        if (pet.State == PetState.InsideHouse || pet.State == PetState.EnteringHouse) return;

        ui.Run(UITween.Punch(pet.Icon.transform, 0.35f, 0.25f));

        // 머리 위로 작은 하트 띄우기
        Image heart = UIFactory.Img(petLayer, "LoveHeart", UIFactory.Red, UIFactory.Heart);
        Vector2 pos = pet.Root.anchoredPosition + new Vector2(0, 56f);
        heart.rectTransform.Center(pos.x, pos.y, 34, 34);
        ui.Run(FloatHeartRoutine(heart));
    }

    static System.Collections.IEnumerator FloatHeartRoutine(Image heart)
    {
        RectTransform rt = heart.rectTransform;
        Vector2 start = rt.anchoredPosition;
        const float duration = 0.65f;

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            if (heart == null) yield break;
            float p = t / duration;
            rt.anchoredPosition = start + new Vector2(0, 48f * p);
            Color c = heart.color;
            c.a = 1f - p;
            heart.color = c;
            yield return null;
        }

        if (heart != null) Object.Destroy(heart.gameObject);
    }

    /// <summary>매 프레임 호출되어 동물들의 이동/집 출입 모션을 업데이트한다.</summary>
    public void Tick()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        for (int i = 0; i < pets.Count; i++)
        {
            UpdatePet(pets[i], dt);
        }
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
                    // 25% 확률로 구석의 집 문으로 놀러 들어감
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
                if (Vector2.Distance(pet.Root.anchoredPosition, pet.TargetPos) < 6f)
                {
                    if (pet.State == PetState.EnteringHouse)
                    {
                        pet.State = PetState.InsideHouse;
                        pet.Timer = Random.Range(2.0f, 4.5f); // 집 안에서 쉬는 시간
                        pet.Group.alpha = 0f;
                        pet.Icon.raycastTarget = false;
                    }
                    else
                    {
                        pet.State = PetState.Idle;
                        pet.Timer = Random.Range(1.2f, 3.2f);
                    }
                }
                break;

            case PetState.InsideHouse:
                pet.Timer -= dt;
                if (pet.Timer <= 0f)
                {
                    // 집 문에서 다시 뿅! 하고 나옴
                    pet.Root.anchoredPosition = doorPos;
                    pet.Group.alpha = 1f;
                    pet.Icon.raycastTarget = true;
                    pet.State = PetState.ExitingHouse;
                    pet.TargetPos = new Vector2(Random.Range(-160f, 260f), Random.Range(yardMin.y, 20f));
                    ui.Run(UITween.PopIn(pet.Icon.transform, 0.3f));
                }
                break;

            case PetState.ExitingHouse:
                MoveTowardsTarget(pet, dt);
                if (Vector2.Distance(pet.Root.anchoredPosition, pet.TargetPos) < 6f)
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

        // 총총총 걷는 바운스 + 뒤뚱뒤뚱 기울기
        pet.BobPhase += dt * 13f;
        float bounce = Mathf.Abs(Mathf.Sin(pet.BobPhase)) * 9f;
        float tilt = Mathf.Sin(pet.BobPhase) * 6f;
        pet.Icon.rectTransform.anchoredPosition = new Vector2(0, bounce);
        pet.Icon.rectTransform.localRotation = Quaternion.Euler(0, 0, tilt);
    }
}
