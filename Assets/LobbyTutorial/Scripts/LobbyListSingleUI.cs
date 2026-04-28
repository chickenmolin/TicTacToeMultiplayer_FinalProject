using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

// 1. Công dụng file:
// - Đại diện UI cho một lobby trong danh sách lobby
// - Hiển thị thông tin lobby (tên, số người, game mode)
// - Cho phép người chơi click để tham gia lobby

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - lobby: dữ liệu lobby hiện tại được hiển thị

// b) UI Components:
// - lobbyNameText: hiển thị tên lobby
// - playersText: hiển thị số người chơi hiện tại / tối đa
// - gameModeText: hiển thị chế độ chơi

// c) UI interaction:
// - Click vào item:
//   + Gọi LobbyManager để join lobby tương ứng

// d) Logic cập nhật:
// - UpdateLobby(lobby):
//   + Gán dữ liệu lobby
//   + Cập nhật toàn bộ UI text theo lobby

// e) Dependencies:
// - LobbyManager: xử lý join lobby
// - Lobby (Unity Services): chứa dữ liệu lobby
// - TextMeshProUGUI: hiển thị text

public class LobbyListSingleUI : MonoBehaviour {
    // Hiển thị thông tin 1 lobby trong danh sách
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playersText;  // VD: "2/4"
    [SerializeField] private TextMeshProUGUI gameModeText;

    private Lobby lobby; // Dữ liệu lobby tương ứng

    private void Awake() {
        // Nhấn vào lobby → tham gia
        GetComponent<Button>().onClick.AddListener(() => {
            LobbyManager.Instance.JoinLobby(lobby);
        });
    }

    // Cập nhật UI với dữ liệu lobby mới
    public void UpdateLobby(Lobby lobby) {
        this.lobby = lobby;
        lobbyNameText.text = lobby.Name;
        playersText.text   = lobby.Players.Count + "/" + lobby.MaxPlayers;
        gameModeText.text  = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;
    }
}
