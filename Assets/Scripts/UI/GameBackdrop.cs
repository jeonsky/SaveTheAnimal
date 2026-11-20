using UnityEngine;

/// <summary>
/// 인게임 배경 연출.
/// - 카메라 배경색을 UI 톤(크림/베이지)에 맞춤
/// - 보드 뒤에 둥근 패널 + 그림자를 깔아 퍼즐 영역을 구분
/// - 화면 비율에 맞춰 카메라를 조정해서 보드가 상단 HUD에 가리지 않게 함
/// </summary>
public class GameBackdrop : MonoBehaviour
{
    public static readonly Color Background = UIFactory.Hex("F6E7D3");
    static readonly Color PanelColor = UIFactory.Hex("FFF8EE");
    static readonly Color ShadowColor = UIFactory.Hex("E3CFB6");

    const float HudReserve = 360f;   // 상단 HUD + 스테이지 표시가 차지하는 높이 (캔버스 단위)
    const float BottomReserve = 80f; // 하단 여백 (캔버스 단위)
    const float Padding = 0.35f;     // 보드와 패널 사이 여백 (월드 단위)

    Camera cam;
    Bounds board;
    Vector2Int lastScreen;

    void Start()
    {
        cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
        }

        if (BoardManager.instance == null) return;
        board = BoardManager.instance.GetBoardBounds();

        CreatePanel("BoardShadow", board.center + new Vector3(0f, -0.12f, 0f), ShadowColor, -11);
        CreatePanel("BoardPanel", board.center, PanelColor, -10);

        FitCamera();
    }

    void LateUpdate()
    {
        // 에디터에서 Game 뷰 크기를 바꾸거나 기기 회전 시 다시 맞춤
        if (Screen.width != lastScreen.x || Screen.height != lastScreen.y) FitCamera();
    }

    void CreatePanel(string name, Vector3 center, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(center.x, center.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = UIFactory.Rounded;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = (Vector2)board.size + Vector2.one * Padding * 2f;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    void FitCamera()
    {
        // 시뮬레이터/기기 시작 직후 화면 크기가 0으로 들어오는 프레임이 있음 → 0으로 나누면 NaN
        if (Screen.width <= 0 || Screen.height <= 0) return;

        lastScreen = new Vector2Int(Screen.width, Screen.height);
        if (cam == null || !cam.orthographic || BoardManager.instance == null) return;

        // UIManager의 CanvasScaler 규칙과 동일: 세로 화면은 가로 1080 기준, 가로 화면은 세로 1920 기준
        float canvasHeight = Screen.width <= Screen.height ? 1080f * Screen.height / Screen.width : 1920f;

        // 펀치홀/노치 등 안전 영역 밖(상단) 비율
        float notch = Mathf.Clamp01((Screen.height - Screen.safeArea.yMax) / Screen.height);

        float top = Mathf.Clamp01(HudReserve / canvasHeight + notch);
        float bottom = Mathf.Clamp01(BottomReserve / canvasHeight);
        float available = Mathf.Max(0.3f, 1f - top - bottom); // 보드가 들어갈 세로 비율

        float aspect = (float)Screen.width / Screen.height;
        Vector2 need = (Vector2)board.size + Vector2.one * (Padding * 2f + 0.3f);

        float sizeByHeight = need.y / available / 2f;
        float sizeByWidth = need.x / aspect / 2f;
        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);

        // 남은 영역의 정중앙에 보드가 오도록 카메라 위치 조정
        float availableCenter = bottom + available / 2f;
        float camY = board.center.y + (0.5f - availableCenter) * 2f * cam.orthographicSize;
        cam.transform.position = new Vector3(board.center.x, camY, cam.transform.position.z);
    }
}
