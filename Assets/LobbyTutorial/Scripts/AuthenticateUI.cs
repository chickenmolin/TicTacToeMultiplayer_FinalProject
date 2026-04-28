using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 1. Công dụng file:
// - Xử lý UI đăng nhập người chơi
// - Gửi tên người chơi lên hệ thống Lobby
// - Ẩn UI sau khi authenticate

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - authenticateButton: nút bấm để xác nhận đăng nhập

// b) UI interaction:
// - Khi click button:
//   + Lấy tên player từ EditPlayerName
//   + Gọi LobbyManager để authenticate
//   + Ẩn UI

// c) Dependencies:
// - LobbyManager: xử lý kết nối / authenticate
// - EditPlayerName: lấy tên người chơi nhập vào

// d) Notes:
// - Sử dụng AddListener để gán sự kiện button
// - UI sẽ bị disable sau khi login

public class AuthenticateUI : MonoBehaviour {
    [SerializeField] private Button authenticateButton; // Nút đăng nhập (gán trong Inspector)

    private void Awake() {
        // Khi nhấn nút → đăng nhập với tên người chơi và ẩn UI
        authenticateButton.onClick.AddListener(() => {
            LobbyManager.Instance.Authenticate(EditPlayerName.Instance.GetPlayerName());
            Hide();
        });
    }

    private void Hide() {
        gameObject.SetActive(false); // Ẩn UI đăng nhập
    }
}
