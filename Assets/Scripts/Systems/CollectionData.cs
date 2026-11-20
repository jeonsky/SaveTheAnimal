using System.Linq;
using UnityEngine;

/// <summary>
/// 동물 도감 / 입양 데이터.
/// - 누적 구조 수가 Goal 이상이 되면 '입양 가능(CanAdopt)' 상태가 된다.
/// - 보호소에서 유저가 직접 입양 버튼을 누르면 '입양 완료(IsAdopted)'로 확정된다.
/// </summary>
public static class CollectionData
{
    static string Key(string spriteName) => "sta_collection_" + spriteName;
    static string AdoptKey(string spriteName) => "sta_adopted_" + spriteName;

    public static int GetCount(string spriteName) => PlayerPrefs.GetInt(Key(spriteName), 0);

    public static void Add(string spriteName, int amount)
    {
        PlayerPrefs.SetInt(Key(spriteName), GetCount(spriteName) + amount);
    }

    public static int Goal(string spriteName) => AnimalCatalog.AdoptGoal(spriteName);

    /// <summary>한 번이라도 구조한 적 있음 (도감에 등록됨)</summary>
    public static bool IsMet(string spriteName) => GetCount(spriteName) > 0;

    /// <summary>목표 구조 수를 채워서 보호소에서 입양 보낼 수 있는 상태</summary>
    public static bool CanAdopt(string spriteName) => !IsAdopted(spriteName) && GetCount(spriteName) >= Goal(spriteName);

    /// <summary>유저가 보호소에서 입양을 확정함 (보드에서 빠짐)</summary>
    public static bool IsAdopted(string spriteName) => PlayerPrefs.GetInt(AdoptKey(spriteName), 0) == 1;

    /// <summary>입양 확정 처리 + 보상(하트 1개 회복)</summary>
    public static bool Adopt(string spriteName)
    {
        if (!CanAdopt(spriteName)) return false;

        PlayerPrefs.SetInt(AdoptKey(spriteName), 1);
        PlayerPrefs.Save();

        HeartSystem.Refund(); // 입양 보상으로 하트 1개 지급
        return true;
    }

    public static int AdoptedCount() => AnimalCatalog.All.Count(s => IsAdopted(s.name));

    public static int CanAdoptCount() => AnimalCatalog.All.Count(s => CanAdopt(s.name));

    public static void ResetEntry(string spriteName)
    {
        PlayerPrefs.DeleteKey(Key(spriteName));
        PlayerPrefs.DeleteKey(AdoptKey(spriteName));
    }

    public static void Save() => PlayerPrefs.Save();
}
