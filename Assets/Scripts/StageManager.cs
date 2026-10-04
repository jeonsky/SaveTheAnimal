using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { Ready, Playing, Paused, Ended }

/// <summary>
/// 스테이지 진행(타이머, 구조 수, 클리어/실패)과 게임 흐름(State)을 담당.
/// UI는 직접 건드리지 않고 이벤트로만 알린다 → 화면은 UIManager가 담당.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    [Header("Stage Settings")]
    public int targetRescueCount = 30;
    public float timeLimit = 60f;

    [Header("Heart Settings")]
    public int maxHearts = 5;
    [Tooltip("하트 1개가 회복되는 시간(초)")]
    public int heartRegenSeconds = 300;

    [Header("Current State (확인용)")]
    public int currentRescueCount = 0;
    public float currentTime;

    public GameState State { get; private set; } = GameState.Ready;
    public bool IsCleared { get; private set; }

    public event Action<GameState> OnStateChanged;
    public event Action<int, int> OnRescueChanged; // (현재 구조 수, 목표 구조 수)

    int[] rescuedByAnimal = new int[0];
    readonly HashSet<int> newlyDiscovered = new HashSet<int>();

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
        ResetState();
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

    /// <summary>보드를 새로 깔고 바로 재시작 (하트 1개 소모).</summary>
    public void Retry()
    {
        ResetState();
        if (BoardManager.instance != null) BoardManager.instance.ResetBoard();

        if (!StartGame()) SetState(GameState.Ready); // 하트가 없으면 타이틀로
    }

    /// <summary>보드를 새로 깔고 타이틀 화면으로.</summary>
    public void GoHome()
    {
        ResetState();
        if (BoardManager.instance != null) BoardManager.instance.ResetBoard();
        SetState(GameState.Ready);
    }

    void ResetState()
    {
        Time.timeScale = 1f;
        State = GameState.Ready;
        IsCleared = false;
        currentTime = timeLimit;
        currentRescueCount = 0;

        int animalCount = (BoardManager.instance != null && BoardManager.instance.animalSprites != null)
            ? BoardManager.instance.animalSprites.Length
            : 0;
        rescuedByAnimal = new int[animalCount];
        newlyDiscovered.Clear();

        OnRescueChanged?.Invoke(currentRescueCount, targetRescueCount);
    }

    // ───────────────────────── 구조 카운트 ─────────────────────────

    /// <summary>동물 종류별 구조 수를 한 번에 반영 (index = animalSprites 인덱스).</summary>
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

        // 클리어하면 하트 반환 → 사실상 "실패했을 때만 하트 차감"
        if (cleared) HeartSystem.Refund();

        SaveCollection();
        SetState(GameState.Ended);

        Debug.Log(cleared ? "게임 클리어!" : "게임 실패...");
    }

    void SaveCollection()
    {
        Sprite[] sprites = BoardManager.instance != null ? BoardManager.instance.animalSprites : null;
        if (sprites == null) return;

        for (int i = 0; i < rescuedByAnimal.Length && i < sprites.Length; i++)
        {
            if (rescuedByAnimal[i] <= 0 || sprites[i] == null) continue;

            bool firstTime = CollectionData.Add(sprites[i].name, rescuedByAnimal[i]);
            if (firstTime) newlyDiscovered.Add(i);
        }

        CollectionData.Save();
    }

    void SetState(GameState next)
    {
        State = next;
        OnStateChanged?.Invoke(next);
    }

    // ───────────────────────── 조회용 ─────────────────────────

    public bool IsPlaying() => State == GameState.Playing;
    public bool IsGameOver() => State == GameState.Ended;
    public int GetRescued(int animalIndex) =>
        animalIndex >= 0 && animalIndex < rescuedByAnimal.Length ? rescuedByAnimal[animalIndex] : 0;
    public bool IsNewDiscovery(int animalIndex) => newlyDiscovered.Contains(animalIndex);

    // ───────────────────────── 테스트용 (Inspector 우클릭 메뉴) ─────────────────────────

    [ContextMenu("Debug/하트 가득 채우기")]
    void DebugRefillHearts() => HeartSystem.SetHearts(HeartSystem.MaxHearts);

    [ContextMenu("Debug/하트 0개로 만들기")]
    void DebugEmptyHearts() => HeartSystem.SetHearts(0);

    [ContextMenu("Debug/도감 초기화")]
    void DebugResetCollection()
    {
        Sprite[] sprites = BoardManager.instance != null ? BoardManager.instance.animalSprites : FindAnyObjectByType<BoardManager>()?.animalSprites;
        if (sprites == null) return;
        foreach (Sprite s in sprites)
        {
            if (s != null) CollectionData.ResetEntry(s.name);
        }
        CollectionData.Save();
    }
}
