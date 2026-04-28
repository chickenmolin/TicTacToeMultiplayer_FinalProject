using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1. Công dụng file:
// - Quản lý tên người chơi trong UI
// - Cho phép người chơi nhập và chỉnh sửa tên
// - Cập nhật tên lên hệ thống Lobby khi thay đổi

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - playerName: tên người chơi hiện tại
// - playerNameText: text hiển thị tên trên UI
// - Instance: singleton để truy cập toàn cục

// b) Event:
// - OnNameChanged: event được gọi khi tên thay đổi

// c) UI interaction:
// - Click vào button:
//   + Mở UI_InputWindow để nhập tên
//   + Giới hạn ký tự và độ dài (max 20)
//   + Khi confirm:
//       * Cập nhật playerName
//       * Update UI text
//       * Trigger OnNameChanged

// d) Logic đồng bộ:
// - Khi OnNameChanged được gọi:
//   + Gửi tên mới lên LobbyManager

// e) Dependencies:
// - UI_InputWindow: popup nhập text
// - LobbyManager: cập nhật tên player lên server
// - TextMeshProUGUI: hiển thị text

// f) Notes:
// - Sử dụng Singleton pattern (Instance)
// - Event giúp tách logic UI và network
// - Tên mặc định: "Code Monkey"

public class EditPlayerName : MonoBehaviour {
    public static EditPlayerName Instance { get; private set; } // Singleton
    public event EventHandler OnNameChanged; // Sự kiện khi tên thay đổi

    [SerializeField] private TextMeshProUGUI playerNameText;
    private string playerName = "Code Monkey"; // Tên mặc định

    private void Awake() {
        Instance = this;

        // Nhấn vào → mở cửa sổ nhập tên
        GetComponent<Button>().onClick.AddListener(() => {
            UI_InputWindow.Show_Static(
                "Player Name", playerName,
                "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-", // Ký tự hợp lệ
                20,          // Giới hạn ký tự
                () => { },   // Cancel → không làm gì
                (string newName) => {
                    playerName = newName;
                    playerNameText.text = playerName;
                    OnNameChanged?.Invoke(this, EventArgs.Empty); // Thông báo tên đã đổi
                }
            );
        });

        playerNameText.text = playerName; // Hiển thị tên mặc định
    }

    private void Start() {
        // Đăng ký: khi tên đổi → cập nhật lên Lobby
        OnNameChanged += EditPlayerName_OnNameChanged;
    }

    private void EditPlayerName_OnNameChanged(object sender, EventArgs e) {
        LobbyManager.Instance.UpdatePlayerName(GetPlayerName()); // Đồng bộ tên lên server
    }

    public string GetPlayerName() { return playerName; }
}
