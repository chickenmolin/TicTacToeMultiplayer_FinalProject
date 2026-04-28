using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;

// 1. Công dụng file:
// - Quản lý việc bắt đầu game sau khi lobby ready
// - Kết nối Relay giữa host và client
// - Khởi tạo Network (Host / Client)

// 2. Các mục quan trọng:

// a) Event:
// - OnLobbyStartGame:
//   + Được gọi khi lobby bắt đầu game
//   + Quyết định chạy Host hoặc Client

// b) Logic start game:
// - Nếu là Host:
//   + Tạo Relay (CreateRelay)
//   + StartHost
// - Nếu là Client:
//   + Join Relay bằng join code
//   + StartClient

// c) Network:
// - StartHost():
//   + Khởi chạy server + client (host)
// - StartClient():
//   + Kết nối tới host qua relay

// d) Relay:
// - CreateRelay():
//   + Tạo allocation trên Relay server
//   + Lấy join code
//   + Set Relay data cho NetworkManager
//   + Start Host
//   + Gửi join code lên LobbyManager

// - JoinRelay(joinCode):
//   + Join vào Relay bằng join code
//   + Set Relay data cho NetworkManager
//   + Start Client

// e) Dependencies:
// - LobbyManager: trigger start game + cung cấp join code
// - RelayService: tạo và join relay
// - NetworkManager (Netcode): quản lý network
// - UnityTransport: cấu hình relay connection

public class StartGameManager : MonoBehaviour {
    private void Start() {
        // Lắng nghe sự kiện bắt đầu game từ LobbyManager
        LobbyManager.Instance.OnLobbyStartGame += LobbyManager_OnLobbyStartGame;
    }

    private void LobbyManager_OnLobbyStartGame(object sender, LobbyManager.LobbyEventArgs e) {
        if (LobbyManager.IsHost)
            CreateRelay();              // Host → tạo phòng Relay
        else
            JoinRelay(LobbyManager.RelayJoinCode); // Client → vào phòng Relay
    }

    public void StartHost()   { NetworkManager.Singleton.StartHost(); }   // Khởi động server + client
    public void StartClient() { NetworkManager.Singleton.StartClient(); } // Khởi động client only

    // Host: tạo Relay allocation → lấy join code → lưu lên lobby cho client
    private async void CreateRelay() {
        try {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); // Tối đa 3 người
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Cấu hình transport dùng DTLS (mã hóa)
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(new RelayServerData(allocation, "dtls"));

            StartHost();
            LobbyManager.Instance.SetRelayJoinCode(joinCode); // Chia sẻ code cho client qua lobby
        } catch (RelayServiceException e) { Debug.Log(e); }
    }

    // Client: nhận join code → kết nối vào Relay của host
    private async void JoinRelay(string joinCode) {
        try {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            StartClient();
        } catch (RelayServiceException e) { Debug.Log(e); }
    }
}
