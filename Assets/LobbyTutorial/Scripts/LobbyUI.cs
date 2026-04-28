using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

// 1. Công dụng file:
// - Quản lý UI lobby khi đã tham gia phòng
// - Hiển thị danh sách player trong lobby
// - Cho phép đổi nhân vật, đổi game mode, rời lobby

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - Instance: singleton để truy cập toàn cục
// - playerSingleTemplate: prefab template cho 1 player
// - container: nơi chứa danh sách player UI

// b) UI Components:
// - lobbyNameText: hiển thị tên lobby
// - playerCountText: hiển thị số người chơi
// - gameModeText: hiển thị chế độ chơi
// - changeMarineButton: chọn nhân vật Marine
// - changeNinjaButton: chọn nhân vật Ninja
// - changeZombieButton: chọn nhân vật Zombie
// - leaveLobbyButton: rời lobby
// - changeGameModeButton: đổi game mode
// - readyButton: nút sẵn sàng (chưa xử lý logic)

// c) UI interaction:
// - Click change character buttons:
//   + Gửi yêu cầu đổi nhân vật lên LobbyManager
// - Click leaveLobbyButton:
//   + Rời lobby
// - Click changeGameModeButton:
//   + Host đổi game mode

// d) Event từ LobbyManager:
// - OnJoinedLobby / OnJoinedLobbyUpdate / OnLobbyGameModeChanged:
//   + Cập nhật UI lobby
// - OnLeftLobby / OnKickedFromLobby:
//   + Clear UI và ẩn lobby

// e) Logic hiển thị:
// - UpdateLobby(lobby):
//   + Xóa danh sách player cũ
//   + Tạo UI cho từng player
//   + Set quyền kick (chỉ host, không kick chính mình)
//   + Cập nhật thông tin lobby (name, player count, game mode)

// f) Utility:
// - ClearLobby():
//   + Xóa toàn bộ player UI (trừ template)
// - Show(): hiển thị UI
// - Hide(): ẩn UI

// g) Dependencies:
// - LobbyManager: cung cấp dữ liệu và xử lý logic lobby
// - LobbyPlayerSingleUI: hiển thị từng player
// - AuthenticationService: lấy player hiện tại
// - Lobby (Unity Services): dữ liệu lobby

public class LobbyUI : MonoBehaviour {
    public static LobbyUI Instance { get; private set; }

    [SerializeField] private Transform playerSingleTemplate; // Template ẩn, dùng để clone
    [SerializeField] private Transform container;            // Chứa danh sách người chơi
    [SerializeField] private TextMeshProUGUI lobbyNameText, playerCountText, gameModeText;

    // Các nút chọn nhân vật, rời lobby, đổi chế độ
    [SerializeField] private Button changeMarineButton, changeNinjaButton, changeZombieButton;
    [SerializeField] private Button leaveLobbyButton, changeGameModeButton, readyButton;

    private void Awake() {
        Instance = this;
        playerSingleTemplate.gameObject.SetActive(false);

        // 3 nút chọn nhân vật → cập nhật lên server
        changeMarineButton.onClick.AddListener(() => LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Marine));
        changeNinjaButton.onClick.AddListener(()  => LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Ninja));
        changeZombieButton.onClick.AddListener(() => LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Zombie));

        leaveLobbyButton.onClick.AddListener(()      => LobbyManager.Instance.LeaveLobby());
        changeGameModeButton.onClick.AddListener(()  => LobbyManager.Instance.ChangeGameMode());
    }

    private void Start() {
        // Lắng nghe các sự kiện lobby để cập nhật UI
        LobbyManager.Instance.OnJoinedLobby          += UpdateLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate     += UpdateLobby_Event;
        LobbyManager.Instance.OnLobbyGameModeChanged  += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby             += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby       += LobbyManager_OnLeftLobby;
        Hide();
    }

    // Rời/bị kick → xóa UI và ẩn đi
    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e) { ClearLobby(); Hide(); }

    private void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e) => UpdateLobby();
    private void UpdateLobby() => UpdateLobby(LobbyManager.Instance.GetJoinedLobby());

    private void UpdateLobby(Lobby lobby) {
        ClearLobby();

        foreach (Player player in lobby.Players) {
            Transform t = Instantiate(playerSingleTemplate, container);
            t.gameObject.SetActive(true);
            LobbyPlayerSingleUI ui = t.GetComponent<LobbyPlayerSingleUI>();

            // Chỉ host mới thấy nút kick, và không được kick chính mình
            ui.SetKickPlayerButtonVisible(
                LobbyManager.Instance.IsLobbyHost() &&
                player.Id != AuthenticationService.Instance.PlayerId
            );
            ui.UpdatePlayer(player);
        }

        // Chỉ host mới thấy nút đổi chế độ chơi
        changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());

        lobbyNameText.text   = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        gameModeText.text    = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;
        Show();
    }

    // Xóa toàn bộ item người chơi, giữ lại template
    private void ClearLobby() {
        foreach (Transform child in container) {
            if (child == playerSingleTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void Hide() { gameObject.SetActive(false); }
    private void Show() { gameObject.SetActive(true); }
}
