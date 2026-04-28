using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

// 1. Công dụng file:
// - Đại diện UI cho một player trong lobby
// - Hiển thị tên và nhân vật của player
// - Cho phép host kick player khỏi lobby

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - player: dữ liệu player hiện tại

// b) UI Components:
// - playerNameText: hiển thị tên player
// - characterImage: hiển thị sprite nhân vật
// - kickPlayerButton: nút kick player

// c) UI interaction:
// - Click kickPlayerButton:
//   + Gọi LobbyManager để kick player khỏi lobby

// d) Logic cập nhật:
// - UpdatePlayer(player):
//   + Gán dữ liệu player
//   + Hiển thị tên player
//   + Lấy character từ data và update sprite

// e) Control UI:
// - SetKickPlayerButtonVisible(visible):
//   + Bật/tắt nút kick (chỉ host thấy)

// f) Dependencies:
// - LobbyManager: xử lý kick player
// - LobbyAssets: cung cấp sprite nhân vật
// - Player (Unity Services): dữ liệu player
// - TextMeshProUGUI / Image: hiển thị UI

public class LobbyPlayerSingleUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Button kickPlayerButton; // Chỉ host mới thấy nút này

    private Player player;

    private void Awake() {
        kickPlayerButton.onClick.AddListener(KickPlayer);
    }

    // Ẩn/hiện nút kick (host = true, client = false)
    public void SetKickPlayerButtonVisible(bool visible) {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    // Cập nhật UI theo dữ liệu người chơi
    public void UpdatePlayer(Player player) {
        this.player = player;
        playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;

        // Lấy sprite nhân vật từ enum
        LobbyManager.PlayerCharacter playerCharacter =
            System.Enum.Parse<LobbyManager.PlayerCharacter>(player.Data[LobbyManager.KEY_PLAYER_CHARACTER].Value);
        characterImage.sprite = LobbyAssets.Instance.GetSprite(playerCharacter);
    }

    private void KickPlayer() {
        if (player != null) LobbyManager.Instance.KickPlayer(player.Id);
    }
}
