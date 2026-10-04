using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 게임에 등장하는 전체 동물 목록.
/// Assets/Resources/Animals/ 폴더의 animal-*.png 를 자동으로 읽는다 → 이미지만 넣으면 새 동물 추가 끝.
/// </summary>
public static class AnimalCatalog
{
    public const string ResourcePath = "Animals";
    public const int BoardAnimalCount = 5; // 한 판에 보드에 나오는 동물 종류 수

    // 등장(해금) 순서. 여기 없는 새 이미지는 맨 뒤에 알파벳순으로 붙는다.
    static readonly string[] UnlockOrder =
    {
        "cat", "chick", "fox", "panda", "polar",
        "bunny", "dog", "pig", "koala", "penguin",
        "monkey", "deer", "cow", "hog", "beaver",
        "parrot", "bee", "caterpillar", "crab", "fish",
        "elephant", "giraffe", "lion", "tiger",
    };

    static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        { "polar", "Polar Bear" },
    };

    static List<Sprite> all;

    /// <summary>해금 순서대로 정렬된 전체 동물</summary>
    public static IReadOnlyList<Sprite> All
    {
        get
        {
            if (all == null || all.Count == 0 || all[0] == null) Load();
            return all;
        }
    }

    static void Load()
    {
        List<Sprite> sprites = Resources.LoadAll<Sprite>(ResourcePath)
            .Where(s => s.name.StartsWith("animal-"))
            .GroupBy(s => s.name)
            .Select(g => g.First())
            .ToList();

        // Resources 폴더를 못 찾으면 BoardManager에 Inspector로 넣어둔 이미지라도 사용
        if (sprites.Count == 0 && BoardManager.instance != null && BoardManager.instance.animalSprites != null)
        {
            sprites = BoardManager.instance.animalSprites.Where(s => s != null).ToList();
        }

        all = sprites.OrderBy(s => OrderOf(Id(s.name))).ThenBy(s => s.name).ToList();
    }

    static int OrderOf(string id)
    {
        int i = System.Array.IndexOf(UnlockOrder, id);
        return i < 0 ? int.MaxValue : i;
    }

    public static string Id(string spriteName) =>
        spriteName.StartsWith("animal-") ? spriteName.Substring("animal-".Length) : spriteName;

    public static int IndexOf(string spriteName)
    {
        IReadOnlyList<Sprite> list = All;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].name == spriteName) return i;
        }
        return -1;
    }

    /// <summary>
    /// 입양 보내는 데 필요한 누적 구조 수.
    /// 해금 순서가 뒤일수록 조금씩 늘어나서, 동물들이 한꺼번에 입양 가지 않고 하나씩 교체된다.
    /// </summary>
    public static int AdoptGoal(string spriteName)
    {
        int index = Mathf.Max(0, IndexOf(spriteName));
        return Mathf.Min(12 + index * 4, 50);
    }

    /// <summary>"animal-polar" → "Polar Bear", "animal-koala" → "Koala"</summary>
    public static string DisplayName(string spriteName)
    {
        string id = Id(spriteName);
        if (DisplayNames.TryGetValue(id, out string name)) return name;

        string[] words = id.Split('-');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0) words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        return string.Join(" ", words);
    }

    /// <summary>
    /// 이번 판 보드에 나올 동물 구성.
    /// 아직 입양 안 간 동물을 해금 순서대로 5종 → 부족하면 입양 간 동물로 빈자리를 채움.
    /// </summary>
    public static Sprite[] BuildRoster()
    {
        var roster = new List<Sprite>();

        foreach (Sprite s in All)
        {
            if (roster.Count >= BoardAnimalCount) break;
            if (!CollectionData.IsAdopted(s.name)) roster.Add(s);
        }

        foreach (Sprite s in All)
        {
            if (roster.Count >= BoardAnimalCount) break;
            if (!roster.Contains(s)) roster.Add(s);
        }

        return roster.ToArray();
    }
}
