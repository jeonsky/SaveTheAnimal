using System;
using UnityEngine;

/// <summary>
/// 하트(라이프) 시스템.
/// - 게임 시작 시 1개 소모, 클리어하면 1개 반환
/// - 최대치 미만이면 일정 시간마다 1개씩 자동 회복 (앱을 꺼도 실제 시간 기준으로 회복)
/// - PlayerPrefs에 저장
/// </summary>
public static class HeartSystem
{
    public static int MaxHearts { get; private set; } = 5;
    public static int RegenSeconds { get; private set; } = 300;

    const string KeyHearts = "sta_hearts";
    const string KeyLastRegen = "sta_heart_last_regen";

    public static void Configure(int maxHearts, int regenSeconds)
    {
        MaxHearts = Mathf.Max(1, maxHearts);
        RegenSeconds = Mathf.Max(1, regenSeconds);
    }

    /// <summary>현재 하트 수 (조회할 때마다 회복 시간 반영)</summary>
    public static int Hearts
    {
        get
        {
            Refresh(out int hearts, out _);
            return hearts;
        }
    }

    /// <summary>다음 하트가 찰 때까지 남은 초. 가득 차 있으면 0.</summary>
    public static int SecondsToNextHeart
    {
        get
        {
            Refresh(out int hearts, out long lastTicks);
            if (hearts >= MaxHearts) return 0;

            double elapsed = (DateTime.UtcNow.Ticks - lastTicks) / (double)TimeSpan.TicksPerSecond;
            return Mathf.Max(0, Mathf.CeilToInt((float)(RegenSeconds - elapsed)));
        }
    }

    public static bool TryUse()
    {
        Refresh(out int hearts, out long lastTicks);
        if (hearts <= 0) return false;

        // 가득 찬 상태에서 쓰면 그 순간부터 회복 타이머 시작
        if (hearts >= MaxHearts) lastTicks = DateTime.UtcNow.Ticks;

        Write(hearts - 1, lastTicks);
        PlayerPrefs.Save();
        return true;
    }

    public static void Refund()
    {
        Refresh(out int hearts, out long lastTicks);
        Write(Mathf.Min(MaxHearts, hearts + 1), lastTicks);
        PlayerPrefs.Save();
    }

    public static void SetHearts(int value)
    {
        Write(Mathf.Clamp(value, 0, MaxHearts), DateTime.UtcNow.Ticks);
        PlayerPrefs.Save();
    }

    // 경과 시간만큼 하트를 회복시키고 현재 값을 돌려준다
    static void Refresh(out int hearts, out long lastTicks)
    {
        long now = DateTime.UtcNow.Ticks;
        hearts = PlayerPrefs.GetInt(KeyHearts, MaxHearts);

        if (!long.TryParse(PlayerPrefs.GetString(KeyLastRegen, ""), out lastTicks) || lastTicks > now)
        {
            lastTicks = now;
        }

        if (hearts >= MaxHearts)
        {
            hearts = Mathf.Min(hearts, MaxHearts);
            lastTicks = now;
            Write(hearts, lastTicks);
            return;
        }

        long regenTicks = TimeSpan.TicksPerSecond * RegenSeconds;
        long gained = (now - lastTicks) / regenTicks;

        if (gained > 0)
        {
            hearts = (int)Math.Min(MaxHearts, hearts + gained);
            lastTicks = hearts >= MaxHearts ? now : lastTicks + gained * regenTicks;
        }

        Write(hearts, lastTicks);
    }

    static void Write(int hearts, long lastTicks)
    {
        PlayerPrefs.SetInt(KeyHearts, hearts);
        PlayerPrefs.SetString(KeyLastRegen, lastTicks.ToString());
    }
}
