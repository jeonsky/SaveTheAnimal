using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x;
    public int y;

    // 기존 colorIndex 이름은 유지
    // 실제 의미는 animalIndex라고 보면 됨
    public int colorIndex;

    public static Tile selectedTile = null;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void OnMouseDown()
    {
        if (BoardManager.instance.isAnimating) return;

        if (selectedTile == null)
        {
            selectedTile = this;
            Highlight(true);
            Debug.Log("선택: (" + x + "," + y + ")");
        }
        else if (selectedTile == this)
        {
            Highlight(false);
            selectedTile = null;
        }
        else
        {
            int dx = Mathf.Abs(x - selectedTile.x);
            int dy = Mathf.Abs(y - selectedTile.y);

            if ((dx == 1 && dy == 0) || (dx == 0 && dy == 1))
            {
                Tile first = selectedTile;
                selectedTile = null;

                first.Highlight(false);

                BoardManager.instance.StartSwap(first, this);
            }
            else
            {
                selectedTile.Highlight(false);

                selectedTile = this;
                Highlight(true);

                Debug.Log("선택 변경: (" + x + "," + y + ")");
            }
        }
    }

    public void Highlight(bool on)
    {
        // 선택 시 살짝 투명하게 표시
        // 선택 해제 시 원래 이미지 색상으로 복귀
        sr.color = on ? new Color(1f, 1f, 1f, 0.6f) : Color.white;
    }

    public void SetSprite(Sprite sprite)
    {
        sr.sprite = sprite;
        sr.color = Color.white;
    }
}