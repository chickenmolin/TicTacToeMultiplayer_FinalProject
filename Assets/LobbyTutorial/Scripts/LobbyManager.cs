using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

// 1. Công dụng file:
// - Quản lý toàn bộ hệ thống Lobby (Unity Services)
// - Xử lý tạo, join, rời lobby
// - Đồng bộ dữ liệu giữa các player
// - Điều khiển start game và chuyển scene

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - Instance: singleton để truy cập toàn cục
// - joinedLobby: lobby hiện tại đang tham gia
// - playerName: tên người chơi
// - IsHost: xác định player có phải host không
// - RelayJoinCode: mã kết nối relay để vào game
// - alreadyStartedGame: trạng thái đã bắt đầu game

// b) Key dữ liệu lobby:
// - KEY_PLAYER_NAME: tên player
// - KEY_PLAYER_CHARACTER: nhân vật player
// - KEY_GAME_MODE: chế độ chơi
// - KEY_START_GAME: trạng thái bắt đầu game
// - KEY_RELAY_JOIN_CODE: mã relay

// c) Event:
// - OnJoinedLobby: khi join lobby
// - OnJoinedLobbyUpdate: khi lobby update
// - OnLeftLobby: khi rời lobby
// - OnKickedFromLobby: khi bị kick
// - OnLobbyGameModeChanged: khi đổi game mode
// - OnLobbyStartGame: khi bắt đầu game
// - OnLobbyListChanged: khi danh sách lobby thay đổi

// d) Lobby lifecycle:
// - Authenticate():
//   + Đăng nhập Unity Services
//   + Lưu playerName
// - CreateLobby():
//   + Tạo lobby mới
// - JoinLobby() / JoinLobbyByCode():
//   + Tham gia lobby
// - LeaveLobby():
//   + Rời lobby
// - QuickJoinLobby():
//   + Join nhanh lobby bất kỳ

// e) Player data:
// - GetPlayer():
//   + Tạo Player object với name + character
// - UpdatePlayerName():
//   + Cập nhật tên player lên lobby
// - UpdatePlayerCharacter():
//   + Cập nhật nhân vật player

// f) Lobby data:
// - ChangeGameMode():
//   + Chuyển đổi game mode
// - UpdateLobbyGameMode():
//   + Sync game mode lên lobby

// g) Lobby system:
// - HandleLobbyHeartbeat():
//   + Host gửi ping để giữ lobby alive
// - HandleLobbyPolling():
//   + Poll dữ liệu lobby liên tục
//   + Cập nhật state lobby
//   + Detect bị kick
//   + Trigger start game khi đủ player

// h) Lobby list:
// - RefreshLobbyList():
//   + Lấy danh sách lobby từ server
//   + Trigger event update UI

// i) Game flow:
// - StartGame():
//   + Host set trạng thái start game
//   + Load scene game
// - JoinGame():
//   + Client join game qua relay code
//   + Load scene game

// j) Relay:
// - SetRelayJoinCode():
//   + Lưu relay code vào lobby để client join

// k) Dependencies:
// - Unity Services (Authentication, Lobby, Relay)
// - SceneManager: load scene game
// - Lobby / Player (Unity Services): dữ liệu lobby

public class LobbyManager : MonoBehaviour {
    public static LobbyManager Instance { get; private set; }
    public static bool IsHost { get; private set; }
    public static string RelayJoinCode { get; private set; }

    // === KEYS lưu dữ liệu lên Unity Lobby Service ===
    public const string KEY_PLAYER_NAME      = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE        = "GameMode";
    public const string KEY_START_GAME       = "StartGame";
    public const string KEY_RELAY_JOIN_CODE  = "RelayJoinCode";

    // === EVENTS thông báo trạng thái lobby ===
    public event EventHandler OnLeftLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;   // Lobby có thay đổi
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;
    public event EventHandler<LobbyEventArgs> OnLobbyStartGame;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;

    public enum GameMode { CaptureTheFlag, Conquest }
    public enum PlayerCharacter { Marine, Ninja, Zombie }

    private float heartbeatTimer;
    private float lobbyPollTimer;
    private Lobby joinedLobby;
    private string playerName;
    private bool alreadyStartedGame;

    private void Update() {
        HandleLobbyHeartbeat(); // Giữ lobby không bị xóa tự động
        HandleLobbyPolling();   // Cập nhật trạng thái lobby định kỳ
    }

    // Đăng nhập ẩn danh với Unity Services
    public async void Authenticate(string playerName) {
        this.playerName = playerName.Replace(" ", "_");
        InitializationOptions options = new InitializationOptions();
        options.SetProfile(playerName);
        await UnityServices.InitializeAsync(options);
        AuthenticationService.Instance.SignedIn += () => RefreshLobbyList();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // Host gửi heartbeat mỗi 15s để lobby không bị xóa
    private async void HandleLobbyHeartbeat() {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                heartbeatTimer = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    // Poll lobby mỗi 1.1s để đồng bộ trạng thái
    private async void HandleLobbyPolling() {
        if (joinedLobby != null) {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f) {
                lobbyPollTimer = 1.1f;
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                // Client nhận relay code → vào game
                if (!IsLobbyHost() && joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value != "")
                    JoinGame(joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value);

                // Host tự động start khi đủ 2 người
                if (!alreadyStartedGame && IsLobbyHost() && joinedLobby.Players.Count == 2)
                    StartGame();

                // Bị kick khỏi lobby
                if (!IsPlayerInLobby()) {
                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                    joinedLobby = null;
                }
            }
        }
    }

    // Tạo player object kèm tên và nhân vật mặc định
    private Player GetPlayer() => new Player(AuthenticationService.Instance.PlayerId, null,
        new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME,      new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
            { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) }
        });

    // Tạo lobby mới với cấu hình đầy đủ
    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode) {
        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, new CreateLobbyOptions {
            Player = GetPlayer(),
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE,       new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, "") }
            }
        });
        joinedLobby = lobby;
        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    // Lấy danh sách lobby còn chỗ trống, mới nhất trước
    public async void RefreshLobbyList() {
        try {
            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync();
            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = response.Results });
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    public async void JoinLobby(Lobby lobby) {
        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id,
            new JoinLobbyByIdOptions { Player = GetPlayer() });
        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void JoinLobbyByCode(string lobbyCode) {
        joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode,
            new JoinLobbyByCodeOptions { Player = GetPlayer() });
        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
    }

    public async void QuickJoinLobby() {
        try {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions());
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    // Cập nhật tên người chơi lên server
    public async void UpdatePlayerName(string playerName) {
        this.playerName = playerName;
        if (joinedLobby == null) return;
        try {
            joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id,
                AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
                    Data = new Dictionary<string, PlayerDataObject> {
                        { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
                    }
                });
            OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    // Cập nhật nhân vật người chơi lên server
    public async void UpdatePlayerCharacter(PlayerCharacter playerCharacter) {
        if (joinedLobby == null) return;
        try {
            joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id,
                AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
                    Data = new Dictionary<string, PlayerDataObject> {
                        { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerCharacter.ToString()) }
                    }
                });
            OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    public async void LeaveLobby() {
        if (joinedLobby == null) return;
        try {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            joinedLobby = null;
            OnLeftLobby?.Invoke(this, EventArgs.Empty);
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    // Chỉ host mới có thể kick người chơi
    public async void KickPlayer(string playerId) {
        if (!IsLobbyHost()) return;
        try {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    public async void UpdateLobbyGameMode(GameMode gameMode) {
        try {
            joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });
            OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    // Host start game: lưu relay code lên lobby → load scene
    public async void StartGame() {
        try {
            joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Public, "1") }
                }
            });
            IsHost = true;
            alreadyStartedGame = true;
            SceneManager.LoadScene(1);
            OnLobbyStartGame?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    // Client nhận relay code và vào game
    private void JoinGame(string relayJoinCode) {
        if (string.IsNullOrEmpty(relayJoinCode)) return;
        IsHost = false;
        RelayJoinCode = relayJoinCode;
        alreadyStartedGame = true;
        SceneManager.LoadScene(1);
        OnLobbyStartGame?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
    }

    // Host lưu relay join code để client có thể kết nối
    public async void SetRelayJoinCode(string relayJoinCode) {
        try {
            joinedLobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });
        } catch (LobbyServiceException e) { Debug.Log(e); }
    }

    public bool IsLobbyHost() => joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    public Lobby GetJoinedLobby() => joinedLobby;

    private bool IsPlayerInLobby() {
        if (joinedLobby?.Players == null) return false;
        foreach (Player player in joinedLobby.Players)
            if (player.Id == AuthenticationService.Instance.PlayerId) return true;
        return false;
    }
}
