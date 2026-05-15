using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    [Header("Stage Settings")]
    public int targetRescueCount = 30;
    public float timeLimit = 60f;

    [Header("Current State")]
    public int currentRescueCount = 0;
    public float currentTime;

    private bool isGameOver = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        currentTime = timeLimit;
    }

    void Update()
    {
        if (isGameOver) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            GameFail();
        }

        Debug.Log("남은 시간: " + Mathf.Ceil(currentTime));
    }

    public void AddRescueCount(int amount)
    {
        if (isGameOver) return;

        currentRescueCount += amount;

        Debug.Log("구조 수: " + currentRescueCount + " / " + targetRescueCount);

        if (currentRescueCount >= targetRescueCount)
        {
            GameClear();
        }
    }

    void GameClear()
    {
        isGameOver = true;

        Debug.Log("게임 클리어!");
    }

    void GameFail()
    {
        isGameOver = true;

        Debug.Log("게임 실패...");
    }
}