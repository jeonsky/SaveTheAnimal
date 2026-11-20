using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 모든 화면(타이틀/HUD/일시정지/결과/도감)을 코드로 생성하고,
/// StageManager의 상태 변화에 맞춰 화면을 전환한다.
/// 씬에 따로 배치하지 않아도 StageManager가 자동으로 생성한다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    HUDView hud;
    TitleView title;
    PauseView pause;
    ResultView result;
    CollectionView collection;

    bool subscribed;
    CanvasScaler canvasScaler;
    RectTransform safeRoot;
    Vector2Int lastScreenSize;
    Rect lastSafeArea;
    StageManager Stage => StageManager.instance;

    void Awake()
    {
        instance = this;

        EnsureEventSystem();
        HideLegacyUI();

        RectTransform root = BuildCanvas();
        ApplyScreenLayout();
        gameObject.AddComponent<GameBackdrop>(); // 인게임 배경/카메라

        // 생성 순서 = 그려지는 순서 (나중에 만든 게 위에 뜸)
        hud = new HUDView(root, this);
        title = new TitleView(root, this);
        pause = new PauseView(root, this);
        result = new ResultView(root, this);
        collection = new CollectionView(root, this);
    }

    void OnEnable() => TrySubscribe();

    void Start()
    {
        TrySubscribe();
        if (Stage != null)
        {
            HandleStateChanged(Stage.State);
            hud.SetRescue(Stage.currentRescueCount, Stage.targetRescueCount);
        }
    }

    void OnDisable()
    {
        if (!subscribed || Stage == null) return;
        Stage.OnStateChanged -= HandleStateChanged;
        Stage.OnRescueChanged -= hud.SetRescue;
        subscribed = false;
    }

    void TrySubscribe()
    {
        if (subscribed || Stage == null || hud == null) return;
        Stage.OnStateChanged += HandleStateChanged;
        Stage.OnRescueChanged += hud.SetRescue;
        subscribed = true;
    }

    void Update()
    {
        ApplyScreenLayout();
        if (Stage == null) return;
        hud.Tick();
        title.Tick();
        pause.Tick();
        result.Tick();
    }

    void HandleStateChanged(GameState state)
    {
        title.SetVisible(state == GameState.Ready);
        hud.SetVisible(state == GameState.Playing || state == GameState.Paused);
        pause.SetVisible(state == GameState.Paused);
        result.SetVisible(state == GameState.Ended);

        if (state == GameState.Playing || state == GameState.Paused) collection.Hide();
    }

    // ───────────────────────── 버튼 액션 ─────────────────────────

    public void StartGame() => Stage.StartGame();
    public void PauseGame() => Stage.Pause();
    public void ResumeGame() => Stage.Resume();
    public void RetryGame() => Stage.Retry();
    public void GoHome() => Stage.GoHome();
    public void OpenCollection() => collection.Show();
    public void CloseCollection() => collection.Hide();

    public Coroutine Run(IEnumerator routine) => StartCoroutine(routine);

    /// <summary>
    /// 화면 크기/안전 영역이 바뀌면 UI 기준을 다시 맞춤.
    /// - 세로 화면: 가로 1080 기준 (길쭉한 폰에서도 UI가 가로를 꽉 채움)
    /// - 가로 화면: 세로 1920 기준 (에디터 가로 Game 뷰 대응)
    /// - 노치/펀치홀을 피하도록 Screen.safeArea 안에 배치
    /// </summary>
    void ApplyScreenLayout()
    {
        if (canvasScaler == null || safeRoot == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        var size = new Vector2Int(Screen.width, Screen.height);
        Rect safe = Screen.safeArea;
        if (size == lastScreenSize && safe == lastSafeArea) return;

        lastScreenSize = size;
        lastSafeArea = safe;

        canvasScaler.matchWidthOrHeight = Screen.width <= Screen.height ? 0f : 1f;

        safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
    }

    // ───────────────────────── 셋업 ─────────────────────────

    RectTransform BuildCanvas()
    {
        var go = new GameObject("UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.GetComponent<CanvasScaler>();
        canvasScaler = scaler;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        // 모든 화면은 안전 영역(노치/펀치홀 제외) 안에 배치
        safeRoot = UIFactory.CreateRect("SafeArea", go.transform);
        return safeRoot;
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    // 예전에 씬에 직접 배치했던 텍스트/결과 패널은 새 UI로 대체되므로 숨김
    static void HideLegacyUI()
    {
        string[] legacyNames = { "TimeText", "RescueText", "ResultPanel" };

        foreach (RectTransform rt in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(legacyNames, rt.name) >= 0)
            {
                rt.gameObject.SetActive(false);
            }
        }
    }
}
