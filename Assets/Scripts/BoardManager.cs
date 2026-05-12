using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public int width = 7;
    public int height = 7;
    public GameObject tilePrefab;
    public bool isAnimating = false;

    public static BoardManager instance;
    private GameObject[,] board;

    private Color[] tileColors = new Color[]
    {
        new Color(1f, 0.4f, 0.4f),
        new Color(0.4f, 0.6f, 1f),
        new Color(0.4f, 0.9f, 0.4f),
        new Color(1f, 0.9f, 0.4f),
        new Color(1f, 0.7f, 0.4f)
    };

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        CreateBoard();
    }

    void CreateBoard()
    {
        Tile.selectedTile = null;
        board = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SpawnTileNoMatch(x, y);
            }
        }
    }

    void SpawnTileNoMatch(int x, int y)
    {
        List<int> available = new List<int> { 0, 1, 2, 3, 4 };

        if (x >= 2)
        {
            Tile left1 = board[x - 1, y]?.GetComponent<Tile>();
            Tile left2 = board[x - 2, y]?.GetComponent<Tile>();

            if (left1 != null && left2 != null && left1.colorIndex == left2.colorIndex)
            {
                available.Remove(left1.colorIndex);
            }
        }

        if (y >= 2)
        {
            Tile down1 = board[x, y - 1]?.GetComponent<Tile>();
            Tile down2 = board[x, y - 2]?.GetComponent<Tile>();

            if (down1 != null && down2 != null && down1.colorIndex == down2.colorIndex)
            {
                available.Remove(down1.colorIndex);
            }
        }

        int colorIdx = available[Random.Range(0, available.Count)];
        Vector3 pos = BoardToWorldPosition(x, y);

        GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
        tile.name = $"Tile({x},{y})";

        Tile tileScript = tile.GetComponent<Tile>();
        tileScript.x = x;
        tileScript.y = y;
        tileScript.colorIndex = colorIdx;
        tileScript.SetColor(GetColor(colorIdx));

        board[x, y] = tile;
    }

    public void StartSwap(Tile a, Tile b)
    {
        if (isAnimating) return;
        StartCoroutine(SwapRoutine(a, b));
    }

    IEnumerator SwapRoutine(Tile a, Tile b)
    {
        isAnimating = true;

        Collider2D colA = a.GetComponent<Collider2D>();
        Collider2D colB = b.GetComponent<Collider2D>();

        colA.enabled = false;
        colB.enabled = false;

        int origAx = a.x;
        int origAy = a.y;
        int origBx = b.x;
        int origBy = b.y;

        Vector3 posA = BoardToWorldPosition(origAx, origAy);
        Vector3 posB = BoardToWorldPosition(origBx, origBy);

        // 1. 먼저 화면상으로 스왑 애니메이션
        yield return StartCoroutine(MoveTo(a.gameObject, posB));
        yield return StartCoroutine(MoveTo(b.gameObject, posA));

        // 2. board/좌표를 스왑 상태로 반영
        a.x = origBx;
        a.y = origBy;
        b.x = origAx;
        b.y = origAy;

        board[a.x, a.y] = a.gameObject;
        board[b.x, b.y] = b.gameObject;

        UpdateTileName(a);
        UpdateTileName(b);

        List<GameObject> matched = FindAllMatches();
        Debug.Log("매치된 타일 수: " + matched.Count);

        // 3. 매치 없으면 원위치 복귀
        if (matched.Count == 0)
        {
            Debug.Log("매치 없음 → 원위치 복귀");

            yield return StartCoroutine(MoveTo(a.gameObject, posA));
            yield return StartCoroutine(MoveTo(b.gameObject, posB));

            a.x = origAx;
            a.y = origAy;
            b.x = origBx;
            b.y = origBy;

            board[a.x, a.y] = a.gameObject;
            board[b.x, b.y] = b.gameObject;

            UpdateTileName(a);
            UpdateTileName(b);

            colA.enabled = true;
            colB.enabled = true;
            isAnimating = false;

            yield break;
        }

        // 4. 매치 있으면 제거
        ClearMatches(matched);

        yield return StartCoroutine(FillBoardRoutine());

        // 5. 리필 후 자동 연쇄 매치 처리
        yield return StartCoroutine(CheckAutoMatchesRoutine());

        if (colA != null) colA.enabled = true;
        if (colB != null) colB.enabled = true;

        isAnimating = false;
    }

    IEnumerator CheckAutoMatchesRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        List<GameObject> matched = FindAllMatches();

        while (matched.Count > 0)
        {
            Debug.Log("자동 연쇄 매치 수: " + matched.Count);

            ClearMatches(matched);

            yield return StartCoroutine(FillBoardRoutine());
            yield return new WaitForSeconds(0.2f);

            matched = FindAllMatches();
        }
    }

    void ClearMatches(List<GameObject> matched)
    {
        foreach (GameObject matchedTile in matched)
        {
            if (matchedTile == null) continue;

            Tile t = matchedTile.GetComponent<Tile>();
            board[t.x, t.y] = null;
            Destroy(matchedTile);
        }
    }

    public List<GameObject> FindAllMatches()
    {
        List<GameObject> matched = new List<GameObject>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x <= width - 3; x++)
            {
                GameObject tile0 = board[x, y];
                GameObject tile1 = board[x + 1, y];
                GameObject tile2 = board[x + 2, y];

                if (tile0 == null || tile1 == null || tile2 == null) continue;

                Tile t0 = tile0.GetComponent<Tile>();
                Tile t1 = tile1.GetComponent<Tile>();
                Tile t2 = tile2.GetComponent<Tile>();

                if (t0.colorIndex == t1.colorIndex && t1.colorIndex == t2.colorIndex)
                {
                    AddMatch(matched, tile0);
                    AddMatch(matched, tile1);
                    AddMatch(matched, tile2);
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= height - 3; y++)
            {
                GameObject tile0 = board[x, y];
                GameObject tile1 = board[x, y + 1];
                GameObject tile2 = board[x, y + 2];

                if (tile0 == null || tile1 == null || tile2 == null) continue;

                Tile t0 = tile0.GetComponent<Tile>();
                Tile t1 = tile1.GetComponent<Tile>();
                Tile t2 = tile2.GetComponent<Tile>();

                if (t0.colorIndex == t1.colorIndex && t1.colorIndex == t2.colorIndex)
                {
                    AddMatch(matched, tile0);
                    AddMatch(matched, tile1);
                    AddMatch(matched, tile2);
                }
            }
        }

        return matched;
    }

    void AddMatch(List<GameObject> matched, GameObject tile)
    {
        if (!matched.Contains(tile))
        {
            matched.Add(tile);
        }
    }

    public IEnumerator FillBoardRoutine()
    {
        List<Coroutine> coroutines = new List<Coroutine>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] != null) continue;

                for (int yAbove = y + 1; yAbove < height; yAbove++)
                {
                    if (board[x, yAbove] == null) continue;

                    board[x, y] = board[x, yAbove];
                    board[x, yAbove] = null;

                    Tile t = board[x, y].GetComponent<Tile>();
                    t.x = x;
                    t.y = y;
                    UpdateTileName(t);

                    Vector3 targetPos = BoardToWorldPosition(x, y);
                    coroutines.Add(StartCoroutine(MoveTo(board[x, y], targetPos)));

                    break;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] != null) continue;

                Vector3 startPos = BoardToWorldPosition(x, height);
                Vector3 targetPos = BoardToWorldPosition(x, y);

                GameObject tile = Instantiate(tilePrefab, startPos, Quaternion.identity);
                tile.name = $"Tile({x},{y})";

                int colorIdx = Random.Range(0, tileColors.Length);

                Tile tileScript = tile.GetComponent<Tile>();
                tileScript.x = x;
                tileScript.y = y;
                tileScript.colorIndex = colorIdx;
                tileScript.SetColor(GetColor(colorIdx));

                board[x, y] = tile;

                coroutines.Add(StartCoroutine(MoveTo(tile, targetPos)));
            }
        }

        foreach (Coroutine c in coroutines)
        {
            yield return c;
        }
    }

    IEnumerator MoveTo(GameObject tile, Vector3 targetPos)
    {
        float speed = 8f;

        while (tile != null && Vector3.Distance(tile.transform.position, targetPos) > 0.01f)
        {
            tile.transform.position = Vector3.MoveTowards(
                tile.transform.position,
                targetPos,
                speed * Time.deltaTime
            );

            yield return null;
        }

        if (tile != null)
        {
            tile.transform.position = targetPos;
        }
    }

    Vector3 BoardToWorldPosition(int x, int y)
    {
        return new Vector3(x * 1.1f, y * 1.1f, 0);
    }

    void UpdateTileName(Tile tile)
    {
        tile.gameObject.name = $"Tile({tile.x},{tile.y})";
    }

    public Color GetColor(int index)
    {
        return tileColors[index];
    }
}