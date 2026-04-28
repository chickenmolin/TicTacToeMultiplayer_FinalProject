using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// 1. Công dụng file:
// - Quản lý UI tạo Lobby
// - Cho phép người chơi thiết lập thông tin lobby
// - Gửi dữ liệu lobby lên LobbyManager để tạo phòng

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - lobbyName: tên lobby
// - isPrivate: trạng thái public / private
// - maxPlayers: số lượng người chơi tối đa
// - gameMode: chế độ chơi
// - Instance: singleton để truy cập toàn cục

// b) UI Components:
// - createButton: nút tạo lobby
// - lobbyNameButton: chỉnh sửa tên lobby
// - publicPrivateButton: chuyển đổi public/private
// - maxPlayersButton: chỉnh sửa số người chơi
// - gameModeButton: chuyển đổi chế độ chơi
// - lobbyNameText: hiển thị tên lobby
// - publicPrivateText: hiển thị trạng thái lobby
// - maxPlayersText: hiển thị số người chơi
// - gameModeText: hiển thị chế độ chơi

// c) UI interaction:
// - Click createButton:
//   + Gửi dữ liệu lobby lên LobbyManager
//   + Ẩn UI
// - Click lobbyNameButton:
//   + Mở UI nhập tên lobby
//   + Cập nhật text
// - Click publicPrivateButton:
//   + Toggle trạng thái public/private
// - Click maxPlayersButton:
//   + Mở UI nhập số người chơi
//   + Cập nhật text
// - Click gameModeButton:
//   + Chuyển đổi giữa các game mode

// d) Logic hiển thị:
// - UpdateText():
//   + Cập nhật toàn bộ text UI theo dữ liệu hiện tại

// e) Show/Hide:
// - Show():
//   + Hiển thị UI
//   + Reset giá trị mặc định
// - Hide():
//   + Ẩn UI

// f) Dependencies:
// - LobbyManager: tạo lobby
// - UI_InputWindow: nhập dữ liệu
// - TextMeshProUGUI: hiển thị text

public class LobbyCreateUI : MonoBehaviour {


    public static LobbyCreateUI Instance { get; private set; }

    // Các nút và text hiển thị trong UI tạo lobby
    [SerializeField] private Button createButton;
    [SerializeField] private Button lobbyNameButton;
    [SerializeField] private Button publicPrivateButton;
    [SerializeField] private Button maxPlayersButton;
    [SerializeField] private Button gameModeButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI publicPrivateText;
    [SerializeField] private TextMeshProUGUI maxPlayersText;
    [SerializeField] private TextMeshProUGUI gameModeText;

    // Cấu hình lobby
    private string lobbyName;
    private bool isPrivate;
    private int maxPlayers;
    private LobbyManager.GameMode gameMode;

    private void Awake() {
        Instance = this;

        // Tạo lobby với cấu hình hiện tại → ẩn UI
        createButton.onClick.AddListener(() => {
            LobbyManager.Instance.CreateLobby(lobbyName, maxPlayers, isPrivate, gameMode);
            Hide();
        });

        // Mở popup nhập tên lobby
        lobbyNameButton.onClick.AddListener(() => {
            UI_InputWindow.Show_Static("Lobby Name", lobbyName, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-", 20,
                () => { }, // Cancel
                (string lobbyName) => { this.lobbyName = lobbyName; UpdateText(); }
            );
        });

        // Toggle Public / Private
        publicPrivateButton.onClick.AddListener(() => {
            isPrivate = !isPrivate;
            UpdateText();
        });

        // Mở popup nhập số người chơi tối đa
        maxPlayersButton.onClick.AddListener(() => {
            UI_InputWindow.Show_Static("Max Players", maxPlayers,
                () => { }, // Cancel
                (int maxPlayers) => { this.maxPlayers = maxPlayers; UpdateText(); }
            );
        });

        // Toggle chế độ chơi giữa CaptureTheFlag ↔ Conquest
        gameModeButton.onClick.AddListener(() => {
            gameMode = gameMode == LobbyManager.GameMode.CaptureTheFlag
                ? LobbyManager.GameMode.Conquest
                : LobbyManager.GameMode.CaptureTheFlag;
            UpdateText();
        });

        Hide();
    }

    // Cập nhật toàn bộ text UI theo giá trị hiện tại
    private void UpdateText() {
        lobbyNameText.text     = lobbyName;
        publicPrivateText.text = isPrivate ? "Private" : "Public";
        maxPlayersText.text    = maxPlayers.ToString();
        gameModeText.text      = gameMode.ToString();
    }


    private void Hide() {
        gameObject.SetActive(false);
    }

    // Hiện UI với giá trị mặc định
    public void Show() {
        gameObject.SetActive(true);

        lobbyName = "MyLobby";
        isPrivate = false;
        maxPlayers = 2;
        gameMode = LobbyManager.GameMode.CaptureTheFlag;

        UpdateText();
    }

}
