using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x;
    public int y;
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
        sr.color = on ? Color.white : BoardManager.instance.GetColor(colorIndex);
    }

    public void SetColor(Color color)
    {
        sr.color = color;
    }
}