using TMPro;
using UnityEngine;

// 1. Công dụng file:
// - Quản lý UI hiển thị thông tin player trong game
// - Hiển thị lượt chơi hiện tại (arrow)
// - Hiển thị điểm số và phân biệt player local

// 2. Các mục quan trọng:

// a) UI Components:
// - crossArrowGameObject: mũi tên chỉ lượt của Cross
// - circleArrowGameObject: mũi tên chỉ lượt của Circle
// - crossYouTextGameObject: text "YOU" cho Cross
// - circleYouTextGameObject: text "YOU" cho Circle
// - playerCrossScoreTextMesh: hiển thị điểm Cross
// - playerCircleScoreTextMesh: hiển thị điểm Circle

// b) Khởi tạo:
// - Tắt toàn bộ arrow và text "YOU"
// - Reset score text

// c) Event từ GameManager:
// - OnGameStarted:
//   + Xác định player local là Cross hay Circle
//   + Hiển thị "YOU" tương ứng
//   + Reset điểm về 0
//   + Cập nhật arrow
// - OnCurrentPlayablePlayerTypeChanged:
//   + Cập nhật arrow theo lượt chơi
// - OnScoreChanged:
//   + Lấy điểm mới từ GameManager
//   + Update UI score

// d) Logic hiển thị:
// - UpdateCurrentArrow():
//   + Hiển thị arrow theo player đang được chơi (Cross / Circle)

// e) Dependencies:
// - GameManager: cung cấp dữ liệu và event gameplay
// - TextMeshProUGUI: hiển thị text
// - GameObject: bật/tắt UI

public class PlayerUI : MonoBehaviour {
    // Mũi tên chỉ lượt hiện tại
    [SerializeField] private GameObject crossArrowGameObject;
    [SerializeField] private GameObject circleArrowGameObject;
    // Text "YOU" hiển thị bên nhân vật của người chơi local
    [SerializeField] private GameObject crossYouTextGameObject;
    [SerializeField] private GameObject circleYouTextGameObject;
    // Điểm số
    [SerializeField] private TextMeshProUGUI playerCrossScoreTextMesh;
    [SerializeField] private TextMeshProUGUI playerCircleScoreTextMesh;

    private void Awake() {
        // Ẩn tất cả cho đến khi game bắt đầu
        crossArrowGameObject.SetActive(false);
        circleArrowGameObject.SetActive(false);
        crossYouTextGameObject.SetActive(false);
        circleYouTextGameObject.SetActive(false);
        playerCrossScoreTextMesh.text = "";
        playerCircleScoreTextMesh.text = "";
    }

    private void Start() {
        GameManager.Instance.OnGameStarted                      += GameManager_OnGameStarted;
        GameManager.Instance.OnCurrentPlayablePlayerTypeChanged += GameManager_OnCurrentPlayablePlayerTypeChanged;
        GameManager.Instance.OnScoreChanged                     += GameManager_OnScoreChanged;
    }

    // Cập nhật điểm số lên UI
    private void GameManager_OnScoreChanged(object sender, System.EventArgs e) {
        GameManager.Instance.GetScores(out int cross, out int circle);
        playerCrossScoreTextMesh.text  = cross.ToString();
        playerCircleScoreTextMesh.text = circle.ToString();
    }

    private void GameManager_OnCurrentPlayablePlayerTypeChanged(object sender, System.EventArgs e)
        => UpdateCurrentArrow();

    // Khi game bắt đầu: hiện "YOU" đúng bên + khởi tạo điểm
    private void GameManager_OnGameStarted(object sender, System.EventArgs e) {
        bool isCross = GameManager.Instance.GetLocalPlayerType() == GameManager.PlayerType.Cross;
        crossYouTextGameObject.SetActive(isCross);
        circleYouTextGameObject.SetActive(!isCross);
        playerCrossScoreTextMesh.text  = "0";
        playerCircleScoreTextMesh.text = "0";
        UpdateCurrentArrow();
    }

    // Mũi tên chỉ vào bên đang có lượt
    private void UpdateCurrentArrow() {
        bool isCrossTurn = GameManager.Instance.GetCurrentPlayablePlayerType() == GameManager.PlayerType.Cross;
        crossArrowGameObject.SetActive(isCrossTurn);
        circleArrowGameObject.SetActive(!isCrossTurn);
    }
}
