using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour
{
    public int x;
    public int y;
    public int colorIndex;

    private static Tile selectedTile = null;
    private static bool isAnimating = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnMouseDown()
{
    if (selectedTile == null)
    {
        selectedTile = this;
        Highlight(true);
        Debug.Log("선택된 타일: (" + x + ", " + y + ")");
    }
    else if (selectedTile == this)
    {
        Highlight(false);
        selectedTile = null;
    }
    else
    {
        if (isAnimating) return; // 스왑 중일 때만 막기
        selectedTile.Highlight(false);
        StartCoroutine(SwapRoutine(selectedTile, this));
        selectedTile = null;
    }
}

    void Highlight(bool on)
    {
        if (on)
            sr.color = Color.white;
        else
            sr.color = BoardManager.instance.GetColor(colorIndex);
    }

    IEnumerator SwapRoutine(Tile a, Tile b)
    {
        isAnimating = true;

        Vector3 originalPosA = a.transform.position;
        Vector3 originalPosB = b.transform.position;

        StartCoroutine(MoveToPos(a.gameObject, originalPosB));
        yield return StartCoroutine(MoveToPos(b.gameObject, originalPosA));

        // colorIndex + 좌표 스왑
        int tempColor = a.colorIndex;
        a.colorIndex = b.colorIndex;
        b.colorIndex = tempColor;

        int tempX = a.x; int tempY = a.y;
        a.x = b.x; a.y = b.y;
        b.x = tempX; b.y = tempY;

        BoardManager.instance.UpdateBoard(a.x, a.y, a.gameObject);
        BoardManager.instance.UpdateBoard(b.x, b.y, b.gameObject);

        // 스왑한 타일 주변만 매치 확인
        var matched = BoardManager.instance.FindMatchesAround(a.x, a.y, b.x, b.y);
        Debug.Log("매치된 타일 수: " + matched.Count);

        if (matched.Count > 0)
        {
            foreach (var tile in matched)
            {
                Tile t = tile.GetComponent<Tile>();
                BoardManager.instance.UpdateBoard(t.x, t.y, null);
                Destroy(tile);
            }
            yield return StartCoroutine(BoardManager.instance.FillBoardRoutine());
        }
        else
        {
            StartCoroutine(MoveToPos(a.gameObject, originalPosA));
            yield return StartCoroutine(MoveToPos(b.gameObject, originalPosB));

            int revertColor = a.colorIndex;
            a.colorIndex = b.colorIndex;
            b.colorIndex = revertColor;

            int revertX = a.x; int revertY = a.y;
            a.x = b.x; a.y = b.y;
            b.x = revertX; b.y = revertY;

            BoardManager.instance.UpdateBoard(a.x, a.y, a.gameObject);
            BoardManager.instance.UpdateBoard(b.x, b.y, b.gameObject);

            Debug.Log("매치 없음 → 제자리로");
        }

        isAnimating = false;
    }

    IEnumerator MoveToPos(GameObject tile, Vector3 targetPos)
    {
        float speed = 8f;
        while (tile != null && Vector3.Distance(tile.transform.position, targetPos) > 0.01f)
        {
            tile.transform.position = Vector3.MoveTowards(tile.transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
        if (tile != null)
            tile.transform.position = targetPos;
    }
}