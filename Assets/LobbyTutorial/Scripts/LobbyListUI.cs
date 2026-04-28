using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

// 1. Công dụng file:
// - Quản lý UI danh sách Lobby
// - Hiển thị danh sách các lobby hiện có
// - Xử lý refresh và tạo lobby mới
// - Ẩn/hiện UI tùy theo trạng thái tham gia lobby

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - Instance: singleton để truy cập toàn cục
// - lobbySingleTemplate: prefab template cho 1 lobby item
// - container: nơi chứa danh sách lobby UI
// - refreshButton: nút refresh danh sách lobby
// - createLobbyButton: nút mở UI tạo lobby

// b) Event từ LobbyManager:
// - OnLobbyListChanged:
//   + Cập nhật lại danh sách lobby
// - OnJoinedLobby:
//   + Ẩn UI khi đã vào lobby
// - OnLeftLobby:
//   + Hiện lại UI khi rời lobby
// - OnKickedFromLobby:
//   + Hiện lại UI khi bị kick

// c) Logic hiển thị danh sách:
// - UpdateLobbyList(lobbyList):
//   + Xóa toàn bộ item cũ (trừ template)
//   + Instantiate item mới cho từng lobby
//   + Gán dữ liệu cho LobbyListSingleUI

// d) UI interaction:
// - RefreshButtonClick():
//   + Gọi LobbyManager để lấy lại danh sách lobby
// - CreateLobbyButtonClick():
//   + Mở UI tạo lobby (LobbyCreateUI)

// e) Show/Hide:
// - Show(): hiển thị UI
// - Hide(): ẩn UI

// f) Dependencies:
// - LobbyManager: cung cấp dữ liệu lobby và event
// - LobbyListSingleUI: hiển thị từng lobby
// - LobbyCreateUI: UI tạo lobby
// - Lobby (Unity Services): dữ liệu lobby

public class LobbyListUI : MonoBehaviour {
    public static LobbyListUI Instance { get; private set; }

    [SerializeField] private Transform lobbySingleTemplate; // Template ẩn, dùng để clone
    [SerializeField] private Transform container;           // Chứa danh sách lobby
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button createLobbyButton;

    private void Awake() {
        Instance = this;
        lobbySingleTemplate.gameObject.SetActive(false); // Ẩn template gốc
        refreshButton.onClick.AddListener(RefreshButtonClick);
        createLobbyButton.onClick.AddListener(CreateLobbyButtonClick);
    }

    private void Start() {
        // Đăng ký lắng nghe các sự kiện từ LobbyManager
        LobbyManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged; // DS lobby thay đổi
        LobbyManager.Instance.OnJoinedLobby      += LobbyManager_OnJoinedLobby;      // Vào lobby → ẩn UI
        LobbyManager.Instance.OnLeftLobby        += LobbyManager_OnLeftLobby;        // Rời lobby → hiện UI
        LobbyManager.Instance.OnKickedFromLobby  += LobbyManager_OnKickedFromLobby;  // Bị kick → hiện UI
    }

    // Cập nhật danh sách lobby: xóa cũ → tạo mới từ template
    private void UpdateLobbyList(List<Lobby> lobbyList) {
        foreach (Transform child in container) {
            if (child == lobbySingleTemplate) continue;
            Destroy(child.gameObject); // Xóa item cũ
        }
        foreach (Lobby lobby in lobbyList) {
            Transform lobbySingleTransform = Instantiate(lobbySingleTemplate, container);
            lobbySingleTransform.gameObject.SetActive(true);
            lobbySingleTransform.GetComponent<LobbyListSingleUI>().UpdateLobby(lobby); // Gán dữ liệu
        }
    }

    private void RefreshButtonClick()     { LobbyManager.Instance.RefreshLobbyList(); }
    private void CreateLobbyButtonClick() { LobbyCreateUI.Instance.Show(); }

    private void LobbyManager_OnLobbyListChanged(object sender, LobbyManager.OnLobbyListChangedEventArgs e)
        => UpdateLobbyList(e.lobbyList);

    private void LobbyManager_OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)  => Hide();
    private void LobbyManager_OnLeftLobby(object sender, EventArgs e)                       => Show();
    private void LobbyManager_OnKickedFromLobby(object sender, LobbyManager.LobbyEventArgs e) => Show();

    private void Hide() { gameObject.SetActive(false); }
    private void Show() { gameObject.SetActive(true); }
}
