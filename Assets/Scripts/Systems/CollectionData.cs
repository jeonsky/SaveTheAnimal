using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 동물 도감 데이터. 동물별 누적 구조 수를 저장하고, 누적 수에 따라 레벨을 매긴다.
/// 키는 스프라이트 이름(animal-cat 등)을 써서 animalSprites 배열 순서가 바뀌어도 안전하다.
/// </summary>
public static class CollectionData
{
    // Lv.1 발견(1마리) → Lv.2(20) → Lv.3(60) → Lv.MAX(150)
    public static readonly int[] LevelThresholds = { 1, 20, 60, 150 };
    public static int MaxLevel => LevelThresholds.Length;

    static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        { "cat", "Cat" },
        { "chick", "Chick" },
        { "fox", "Fox" },
        { "panda", "Panda" },
        { "polar", "Polar Bear" },
    };

    static string Key(string animalId) => "sta_collection_" + animalId;

    public static int GetCount(string animalId) => PlayerPrefs.GetInt(Key(animalId), 0);

    /// <summary>구조 수 누적. 이번에 처음 발견했으면 true.</summary>
    public static bool Add(string animalId, int amount)
    {
        int before = GetCount(animalId);
        PlayerPrefs.SetInt(Key(animalId), before + amount);
        return before == 0 && amount > 0;
    }

    public static void ResetEntry(string animalId) => PlayerPrefs.DeleteKey(Key(animalId));

    public static void Save() => PlayerPrefs.Save();

    public static int GetLevel(int count)
    {
        int level = 0;
        foreach (int threshold in LevelThresholds)
        {
            if (count >= threshold) level++;
        }
        return level;
    }

    /// <summary>다음 레벨 기준치. 최대 레벨이면 -1.</summary>
    public static int NextThreshold(int count)
    {
        foreach (int threshold in LevelThresholds)
        {
            if (count < threshold) return threshold;
        }
        return -1;
    }

    /// <summary>현재 레벨 기준치 (진행도 바 시작점).</summary>
    public static int CurrentThreshold(int count)
    {
        int result = 0;
        foreach (int threshold in LevelThresholds)
        {
            if (count >= threshold) result = threshold;
        }
        return result;
    }

    /// <summary>"animal-polar" → "Polar Bear"</summary>
    public static string DisplayName(string spriteName)
    {
        string id = spriteName.StartsWith("animal-") ? spriteName.Substring("animal-".Length) : spriteName;
        if (DisplayNames.TryGetValue(id, out string name)) return name;
        return id.Length > 0 ? char.ToUpper(id[0]) + id.Substring(1) : id;
    }
}
