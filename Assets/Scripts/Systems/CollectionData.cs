using System.Linq;
using UnityEngine;

/// <summary>
/// 동물 도감 / 입양 데이터.
/// 동물별 누적 구조 수를 저장하고, 목표치(AdoptGoal)를 채우면 "입양 완료"로 본다.
/// 키는 스프라이트 이름(animal-cat 등)이라 이미지 순서가 바뀌어도 안전하다.
/// </summary>
public static class CollectionData
{
    static string Key(string spriteName) => "sta_collection_" + spriteName;

    public static int GetCount(string spriteName) => PlayerPrefs.GetInt(Key(spriteName), 0);

    public static void Add(string spriteName, int amount)
    {
        PlayerPrefs.SetInt(Key(spriteName), GetCount(spriteName) + amount);
    }

    public static int Goal(string spriteName) => AnimalCatalog.AdoptGoal(spriteName);

    /// <summary>한 번이라도 구조한 적 있음 (도감에 등록됨)</summary>
    public static bool IsMet(string spriteName) => GetCount(spriteName) > 0;

    /// <summary>입양 완료 (보드에서 빠짐)</summary>
    public static bool IsAdopted(string spriteName) => GetCount(spriteName) >= Goal(spriteName);

    public static int AdoptedCount() => AnimalCatalog.All.Count(s => IsAdopted(s.name));

    public static void ResetEntry(string spriteName) => PlayerPrefs.DeleteKey(Key(spriteName));

    public static void Save() => PlayerPrefs.Save();
}
