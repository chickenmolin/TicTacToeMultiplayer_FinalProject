using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1. Công dụng file:
// - Quản lý UI hiển thị kết quả game (Win / Lose / Tie)
// - Cho phép người chơi chơi lại (Rematch)

// 2. Các mục quan trọng:

// a) UI Components:
// - resultTextMesh: hiển thị kết quả (Win / Lose / Tie)
// - winColor: màu khi thắng
// - loseColor: màu khi thua
// - tieColor: màu khi hòa
// - rematchButton: nút chơi lại

// b) UI interaction:
// - Click rematchButton:
//   + Gửi yêu cầu rematch lên GameManager (RPC)

// c) Event từ GameManager:
// - OnGameWin:
//   + Hiển thị "YOU WIN" hoặc "YOU LOSE"
//   + Đổi màu tương ứng
// - OnGameTied:
//   + Hiển thị "TIE"
//   + Đổi màu tie
// - OnRematch:
//   + Ẩn UI

// d) Logic hiển thị:
// - Show(): hiển thị UI kết quả
// - Hide(): ẩn UI

// e) Dependencies:
// - GameManager: cung cấp event và xử lý logic game
// - TextMeshProUGUI: hiển thị text
// - Button: xử lý input UI

public class GameOverUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI resultTextMesh;
    [SerializeField] private Color winColor, loseColor, tieColor; // Màu theo kết quả
    [SerializeField] private Button rematchButton;

    private void Awake() {
        rematchButton.onClick.AddListener(() => GameManager.Instance.RematchRpc()); // Gửi yêu cầu chơi lại lên server
    }

    private void Start() {
        GameManager.Instance.OnGameWin  += GameManager_OnGameWin;
        GameManager.Instance.OnRematch  += GameManager_OnRematch;  // Chơi lại → ẩn UI
        GameManager.Instance.OnGameTied += GameManager_OnGameTied;
        Hide();
    }

    private void GameManager_OnGameTied(object sender, System.EventArgs e) {
        resultTextMesh.text  = "TIE!";
        resultTextMesh.color = tieColor;
        Show();
    }

    private void GameManager_OnRematch(object sender, System.EventArgs e) => Hide();

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e) {
        // So sánh người thắng với người chơi local
        bool isLocalPlayerWin = e.winPlayerType == GameManager.Instance.GetLocalPlayerType();
        resultTextMesh.text  = isLocalPlayerWin ? "YOU WIN!" : "YOU LOSE!";
        resultTextMesh.color = isLocalPlayerWin ? winColor   : loseColor;
        Show();
    }

    private void Show() { gameObject.SetActive(true); }
    private void Hide() { gameObject.SetActive(false); }
}
