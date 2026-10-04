using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { Ready, Playing, Paused, Ended }

/// <summary>
/// 스테이지 진행(타이머, 구조 수, 클리어/실패), 스테이지 번호, 입양 처리, 게임 흐름(State)을 담당.
/// UI는 직접 건드리지 않고 이벤트로만 알린다 → 화면은 UIManager가 담당.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    [Header("Stage Settings")]
    [Tooltip("스테이지 1의 구조 목표")]
    public int baseTargetRescue = 30;
    [Tooltip("스테이지가 오를 때마다 늘어나는 구조 목표")]
    public int targetIncreasePerStage = 3;
    [Tooltip("구조 목표 최대치")]
    public int maxTargetRescue = 60;
    public float timeLimit = 60f;

    [Header("Heart Settings")]
    public int maxHearts = 5;
    [Tooltip("하트 1개가 회복되는 시간(초)")]
    public int heartRegenSeconds = 300;

    [Header("Current State (확인용)")]
    public int targetRescueCount = 30;
    public int currentRescueCount = 0;
    public float currentTime;

    public GameState State { get; private set; } = GameState.Ready;
    public bool IsCleared { get; private set; }

    /// <summary>저장된 진행 스테이지 (클리어하면 +1)</summary>
    public int CurrentStage => Mathf.Max(1, PlayerPrefs.GetInt(KeyStage, 1));
    /// <summary>지금 플레이 중인(또는 방금 끝난) 스테이지</summary>
    public int PlayedStage { get; private set; } = 1;
    /// <summary>이번 판 보드에 나오는 동물들</summary>
    public Sprite[] Roster { get; private set; } = new Sprite[0];
    /// <summary>다음 판에 새로 합류하는 동물들 (게임 종료 시 계산)</summary>
    public IReadOnlyList<Sprite> NewFriends => newFriends;

    public event Action<GameState> OnStateChanged;
    public event Action<int, int> OnRescueChanged; // (현재 구조 수, 목표 구조 수)

    const string KeyStage = "sta_stage";

    int[] rescuedByAnimal = new int[0];
    readonly HashSet<int> newlyMet = new HashSet<int>();
    readonly HashSet<int> adoptedNow = new HashSet<int>();
    readonly List<Sprite> newFriends = new List<Sprite>();

    void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
        HeartSystem.Configure(maxHearts, heartRegenSeconds);

        // 씬에 UIManager가 없으면 자동 생성 (씬 수작업 없이 UI가 붙도록)
        if (FindAnyObjectByType<UIManager>() == null)
        {
            new GameObject("UIManager").AddComponent<UIManager>();
        }
    }

    void Start()
    {
        PrepareStage();
        SetState(GameState.Ready);
    }

    void Update()
    {
        if (State != GameState.Playing) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            EndGame(false);
        }
    }

    // 모바일에서 앱이 백그라운드로 가면 자동 일시정지
    void OnApplicationPause(bool paused)
    {
        if (paused && State == GameState.Playing) Pause();
    }

    // ───────────────────────── 게임 흐름 ─────────────────────────

    /// <summary>하트 1개를 소모하고 게임 시작. 하트가 없으면 false.</summary>
    public bool StartGame()
    {
        if (State != GameState.Ready) return false;
        if (!HeartSystem.TryUse()) return false;

        SetState(GameState.Playing);
        return true;
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    /// <summary>현재 스테이지를 새로 깔고 바로 시작 (클리어 후라면 = 다음 스테이지). 하트 1개 소모.</summary>
    public void Retry()
    {
        PrepareStage();
        if (!StartGame()) SetState(GameState.Ready); // 하트가 없으면 타이틀로
    }

    /// <summary>현재 스테이지를 새로 깔고 타이틀 화면으로.</summary>
    public void GoHome()
    {
        PrepareStage();
        SetState(GameState.Ready);
    }

    /// <summary>스테이지 번호에 맞게 목표/동물 구성/보드를 준비</summary>
    void PrepareStage()
    {
        Time.timeScale = 1f;
        State = GameState.Ready;
        IsCleared = false;

        PlayedStage = CurrentStage;
        targetRescueCount = Mathf.Min(maxTargetRescue, baseTargetRescue + (PlayedStage - 1) * targetIncreasePerStage);
        currentTime = timeLimit;
        currentRescueCount = 0;

        // 입양 안 간 동물 위주로 이번 판 구성
        Roster = AnimalCatalog.BuildRoster();
        BoardManager board = BoardManager.instance;
        if (board != null)
        {
            if (Roster.Length >= 3) board.animalSprites = Roster;
            else Roster = board.animalSprites; // 이미지가 부족하면 Inspector 설정 그대로 사용
            board.ResetBoard();
        }

        rescuedByAnimal = new int[Roster.Length];
        newlyMet.Clear();
        adoptedNow.Clear();
        newFriends.Clear();

        OnRescueChanged?.Invoke(currentRescueCount, targetRescueCount);
    }

    // ───────────────────────── 구조 카운트 ─────────────────────────

    /// <summary>동물 종류별 구조 수를 한 번에 반영 (index = Roster 인덱스).</summary>
    public void AddRescue(int[] countsByAnimal)
    {
        if (State != GameState.Playing || countsByAnimal == null) return;

        if (rescuedByAnimal.Length < countsByAnimal.Length)
        {
            Array.Resize(ref rescuedByAnimal, countsByAnimal.Length);
        }

        int sum = 0;
        for (int i = 0; i < countsByAnimal.Length; i++)
        {
            rescuedByAnimal[i] += countsByAnimal[i];
            sum += countsByAnimal[i];
        }

        currentRescueCount += sum;
        OnRescueChanged?.Invoke(currentRescueCount, targetRescueCount);

        if (currentRescueCount >= targetRescueCount)
        {
            EndGame(true);
        }
    }

    void EndGame(bool cleared)
    {
        if (State == GameState.Ended) return;

        IsCleared = cleared;
        Time.timeScale = 1f;

        if (cleared)
        {
            HeartSystem.Refund();                         // 클리어하면 하트 반환
            PlayerPrefs.SetInt(KeyStage, PlayedStage + 1); // 자동으로 다음 스테이지
        }

        // 기획: 클리어해야만 구조한 동물을 보호소에 데려갈 수 있음 (실패 시 저장 X)
        if (cleared)
        {
            SaveCollection();
            FindNewFriends();
        }
        PlayerPrefs.Save();

        SetState(GameState.Ended);
        Debug.Log(cleared ? "게임 클리어!" : "게임 실패...");
    }

    void SaveCollection()
    {
        for (int i = 0; i < rescuedByAnimal.Length && i < Roster.Length; i++)
        {
            if (rescuedByAnimal[i] <= 0 || Roster[i] == null) continue;

            string id = Roster[i].name;
            int before = CollectionData.GetCount(id);
            int goal = CollectionData.Goal(id);

            CollectionData.Add(id, rescuedByAnimal[i]);

            if (before == 0) newlyMet.Add(i);
            if (before < goal && before + rescuedByAnimal[i] >= goal) adoptedNow.Add(i);
        }
    }

    // 입양 간 자리에 다음 판부터 처음 등장하는 동물
    void FindNewFriends()
    {
        foreach (Sprite s in AnimalCatalog.BuildRoster())
        {
            if (Array.IndexOf(Roster, s) < 0 && !CollectionData.IsMet(s.name)) newFriends.Add(s);
        }
    }

    void SetState(GameState next)
    {
        State = next;
        OnStateChanged?.Invoke(next);
    }

    // ───────────────────────── 조회용 ─────────────────────────

    public bool IsPlaying() => State == GameState.Playing;
    public bool IsGameOver() => State == GameState.Ended;
    public int GetRescued(int rosterIndex) =>
        rosterIndex >= 0 && rosterIndex < rescuedByAnimal.Length ? rescuedByAnimal[rosterIndex] : 0;
    public bool IsNewDiscovery(int rosterIndex) => newlyMet.Contains(rosterIndex);
    public bool WasAdoptedNow(int rosterIndex) => adoptedNow.Contains(rosterIndex);
    public int AdoptedNowCount => adoptedNow.Count;

    // ───────────────────────── 테스트용 (Inspector 우클릭 메뉴) ─────────────────────────

    [ContextMenu("Debug/하트 가득 채우기")]
    void DebugRefillHearts() => HeartSystem.SetHearts(HeartSystem.MaxHearts);

    [ContextMenu("Debug/하트 0개로 만들기")]
    void DebugEmptyHearts() => HeartSystem.SetHearts(0);

    [ContextMenu("Debug/진행도 전체 초기화 (스테이지 + 도감)")]
    void DebugResetProgress()
    {
        foreach (Sprite s in AnimalCatalog.All) CollectionData.ResetEntry(s.name);
        PlayerPrefs.DeleteKey(KeyStage);
        PlayerPrefs.Save();
        Debug.Log("진행도 초기화 완료. 다시 Play 해주세요.");
    }

    [ContextMenu("Debug/보드 동물 전부 입양 직전으로")]
    void DebugAlmostAdopt()
    {
        foreach (Sprite s in AnimalCatalog.BuildRoster())
        {
            int need = CollectionData.Goal(s.name) - 1 - CollectionData.GetCount(s.name);
            if (need > 0) CollectionData.Add(s.name, need);
        }
        PlayerPrefs.Save();
        Debug.Log("이번 판에서 한 마리만 더 구하면 입양! (진행 중이면 다음 판부터 반영)");
    }
}
