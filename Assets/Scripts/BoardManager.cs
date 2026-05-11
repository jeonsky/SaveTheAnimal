using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public int width = 7;
    public int height = 7;
    public GameObject tilePrefab;

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
        board = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * 1.1f, y * 1.1f, 0);
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
                tile.name = "Tile(" + x + "," + y + ")";

                int randomIndex = Random.Range(0, tileColors.Length);
                tile.GetComponent<SpriteRenderer>().color = tileColors[randomIndex];

                Tile tileScript = tile.GetComponent<Tile>();
                tileScript.x = x;
                tileScript.y = y;
                tileScript.colorIndex = randomIndex;

                board[x, y] = tile;
            }
        }
    }

    public void UpdateBoard(int x, int y, GameObject tile)
    {
        board[x, y] = tile;
    }

    public List<GameObject> FindMatchesAround(int ax, int ay, int bx, int by)
    {
        List<GameObject> matched = new List<GameObject>();

        for (int y = 0; y < height; y++)
        {
            if (y != ay && y != by) continue;
            for (int x = 0; x <= width - 3; x++)
            {
                if (board[x,y] == null || board[x+1,y] == null || board[x+2,y] == null) continue;
                Tile t0 = board[x,y].GetComponent<Tile>();
                Tile t1 = board[x+1,y].GetComponent<Tile>();
                Tile t2 = board[x+2,y].GetComponent<Tile>();
                if (t0.colorIndex == t1.colorIndex && t1.colorIndex == t2.colorIndex)
                {
                    if (!matched.Contains(board[x,y])) matched.Add(board[x,y]);
                    if (!matched.Contains(board[x+1,y])) matched.Add(board[x+1,y]);
                    if (!matched.Contains(board[x+2,y])) matched.Add(board[x+2,y]);
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            if (x != ax && x != bx) continue;
            for (int y = 0; y <= height - 3; y++)
            {
                if (board[x,y] == null || board[x,y+1] == null || board[x,y+2] == null) continue;
                Tile t0 = board[x,y].GetComponent<Tile>();
                Tile t1 = board[x,y+1].GetComponent<Tile>();
                Tile t2 = board[x,y+2].GetComponent<Tile>();
                if (t0.colorIndex == t1.colorIndex && t1.colorIndex == t2.colorIndex)
                {
                    if (!matched.Contains(board[x,y])) matched.Add(board[x,y]);
                    if (!matched.Contains(board[x,y+1])) matched.Add(board[x,y+1]);
                    if (!matched.Contains(board[x,y+2])) matched.Add(board[x,y+2]);
                }
            }
        }

        return matched;
    }

    public void FillBoard()
    {
        StartCoroutine(FillBoardRoutine());
    }

    public IEnumerator FillBoardRoutine()
    {
        List<Coroutine> coroutines = new List<Coroutine>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] == null)
                {
                    for (int yAbove = y + 1; yAbove < height; yAbove++)
                    {
                        if (board[x, yAbove] != null)
                        {
                            board[x, y] = board[x, yAbove];
                            board[x, yAbove] = null;

                            Tile t = board[x, y].GetComponent<Tile>();
                            t.y = y;
                            board[x, y].transform.position = new Vector3(x * 1.1f, yAbove * 1.1f, 0);
                            coroutines.Add(StartCoroutine(MoveTile(board[x, y], new Vector3(x * 1.1f, y * 1.1f, 0))));
                            break;
                        }
                    }
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] == null)
                {
                    Vector3 startPos = new Vector3(x * 1.1f, height * 1.1f, 0);
                    GameObject tile = Instantiate(tilePrefab, startPos, Quaternion.identity);
                    tile.name = "Tile(" + x + "," + y + ")";

                    int randomIndex = Random.Range(0, tileColors.Length);
                    tile.GetComponent<SpriteRenderer>().color = tileColors[randomIndex];

                    Tile tileScript = tile.GetComponent<Tile>();
                    tileScript.x = x;
                    tileScript.y = y;
                    tileScript.colorIndex = randomIndex;

                    board[x, y] = tile;
                    coroutines.Add(StartCoroutine(MoveTile(tile, new Vector3(x * 1.1f, y * 1.1f, 0))));
                }
            }
        }

        foreach (var c in coroutines)
            yield return c;
    }

    IEnumerator MoveTile(GameObject tile, Vector3 targetPos)
    {
        float speed = 5f;
        while (tile != null && Vector3.Distance(tile.transform.position, targetPos) > 0.01f)
        {
            tile.transform.position = Vector3.MoveTowards(tile.transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }
        if (tile != null)
            tile.transform.position = targetPos;
    }

    public Color GetColor(int index)
    {
        return tileColors[index];
    }
}