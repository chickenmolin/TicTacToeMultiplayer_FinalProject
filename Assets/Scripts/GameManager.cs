using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// 1. Công dụng file:
// - Quản lý logic game Tic Tac Toe (multiplayer)
// - Đồng bộ trạng thái game qua network (Netcode)
// - Xử lý lượt chơi, kiểm tra thắng/thua/hòa
// - Quản lý điểm số và rematch

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - Instance: singleton để truy cập toàn cục
// - localPlayerType: loại player của client hiện tại (Cross / Circle)
// - currentPlayablePlayerType: lượt chơi hiện tại (NetworkVariable)
// - playerTypeArray: ma trận 3x3 lưu trạng thái bàn cờ
// - lineList: danh sách các đường thắng (horizontal, vertical, diagonal)
// - playerCrossScore / playerCircleScore: điểm số 2 bên (NetworkVariable)

// b) Enum:
// - PlayerType: None / Cross / Circle
// - Orientation: hướng của đường thắng (Horizontal, Vertical, Diagonal)

// c) Event:
// - OnGameStarted: khi bắt đầu game
// - OnClickedOnGridPosition: khi player click ô
// - OnPlacedObject: khi đặt X/O
// - OnGameWin: khi có người thắng
// - OnGameTied: khi hòa
// - OnRematch: khi chơi lại
// - OnCurrentPlayablePlayerTypeChanged: khi đổi lượt
// - OnScoreChanged: khi thay đổi điểm

// d) Network logic:
// - OnNetworkSpawn():
//   + Xác định player là Cross hay Circle
//   + Subscribe event NetworkVariable
// - RPC:
//   + ClickedOnGridPositionRpc(): gửi input lên server
//   + TriggerOnGameStartedRpc(): bắt đầu game
//   + TriggerOnPlacedObjectRpc(): sync đặt X/O
//   + TriggerOnGameWinRpc(): sync thắng
//   + TriggerOnGameTiedRpc(): sync hòa
//   + RematchRpc(): reset game
//   + TriggerOnRematchRpc(): sync rematch

// e) Gameplay logic:
// - ClickedOnGridPositionRpc():
//   + Kiểm tra hợp lệ (đúng lượt, ô trống)
//   + Gán giá trị vào board
//   + Đổi lượt chơi
//   + Kiểm tra thắng/hòa

// - TestWinner():
//   + Duyệt tất cả line để kiểm tra thắng
//   + Nếu thắng: cập nhật điểm + trigger event
//   + Nếu full bàn: hòa

// f) Win condition:
// - TestWinnerLine():
//   + Kiểm tra 3 ô cùng loại và khác None

// g) Game flow:
// - Khi đủ 2 player:
//   + Start game
//   + Cross đi trước
// - Rematch:
//   + Reset board
//   + Reset lượt chơi

// h) Score:
// - Tăng điểm khi có player thắng
// - Sync qua NetworkVariable

// i) Dependencies:
// - NetworkBehaviour / Netcode: đồng bộ multiplayer
// - NetworkManager: quản lý client/server
// - UnityEngine: core engine

// NetworkBehaviour = đồng bộ qua mạng (Netcode for GameObjects)
public class GameManager : NetworkBehaviour {
    public static GameManager Instance { get; private set; }

    // === EVENTS thông báo trạng thái game ===
    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public event EventHandler OnGameStarted;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnScoreChanged;
    public event EventHandler OnPlacedObject;

    public enum PlayerType { None, Cross, Circle }
    public enum Orientation { Horizontal, Vertical, DiagonalA, DiagonalB }

    // Struct mô tả 1 đường thắng (3 ô liên tiếp)
    public struct Line {
        public List<Vector2Int> gridVector2IntList; // 3 tọa độ ô
        public Vector2Int centerGridPosition;        // Ô giữa
        public Orientation orientation;
    }

    private PlayerType localPlayerType;

    // NetworkVariable: tự đồng bộ giá trị giữa server và client
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private NetworkVariable<int> playerCrossScore  = new NetworkVariable<int>();
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();

    private PlayerType[,] playerTypeArray; // Bảng 3x3 lưu trạng thái ô
    private List<Line> lineList;           // 8 đường thắng có thể (3H + 3V + 2D)

    private void Awake() {
        Instance = this;
        playerTypeArray = new PlayerType[3, 3];

        // Định nghĩa sẵn 8 đường thắng: 3 ngang, 3 dọc, 2 chéo
        lineList = new List<Line> { /* ... 8 lines ... */ };
    }

    public override void OnNetworkSpawn() {
        // Client 0 = Cross, Client 1 = Circle
        localPlayerType = NetworkManager.Singleton.LocalClientId == 0
            ? PlayerType.Cross : PlayerType.Circle;

        // Chỉ server lắng nghe khi có client kết nối
        if (IsServer)
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;

        // Lắng nghe thay đổi NetworkVariable → bắn event UI
        currentPlayablePlayerType.OnValueChanged += (old, newVal) => OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
        playerCrossScore.OnValueChanged  += (old, newVal) => OnScoreChanged?.Invoke(this, EventArgs.Empty);
        playerCircleScore.OnValueChanged += (old, newVal) => OnScoreChanged?.Invoke(this, EventArgs.Empty);
    }

    // Đủ 2 người → server bắt đầu game
    private void NetworkManager_OnClientConnectedCallback(ulong obj) {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2) {
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }

    // RPC gửi đến tất cả (Server + Client)
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc() => OnGameStarted?.Invoke(this, EventArgs.Empty);

    // Client gửi lên server khi click ô
    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType) {
        if (playerType != currentPlayablePlayerType.Value) return; // Không phải lượt của mình
        if (playerTypeArray[x, y] != PlayerType.None) return;      // Ô đã bị chiếm

        playerTypeArray[x, y] = playerType;
        TriggerOnPlacedObjectRpc();
        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs { x = x, y = y, playerType = playerType });

        // Đổi lượt
        currentPlayablePlayerType.Value = (currentPlayablePlayerType.Value == PlayerType.Cross)
            ? PlayerType.Circle : PlayerType.Cross;

        TestWinner();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlacedObjectRpc() => OnPlacedObject?.Invoke(this, EventArgs.Empty);

    // Kiểm tra 3 ô cùng loại và khác None
    private bool TestWinnerLine(Line line) => TestWinnerLine(
        playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
        playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
        playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]);

    private bool TestWinnerLine(PlayerType a, PlayerType b, PlayerType c)
        => a != PlayerType.None && a == b && b == c;

    private void TestWinner() {
        // Kiểm tra 8 đường thắng
        for (int i = 0; i < lineList.Count; i++) {
            if (TestWinnerLine(lineList[i])) {
                currentPlayablePlayerType.Value = PlayerType.None; // Dừng game
                PlayerType winner = playerTypeArray[lineList[i].centerGridPosition.x, lineList[i].centerGridPosition.y];
                if (winner == PlayerType.Cross)  playerCrossScore.Value++;
                else                             playerCircleScore.Value++;
                TriggerOnGameWinRpc(i, winner);
                return;
            }
        }

        // Hòa khi tất cả ô đều đã điền
        bool hasTie = true;
        foreach (PlayerType cell in playerTypeArray)
            if (cell == PlayerType.None) { hasTie = false; break; }
        if (hasTie) TriggerOnGameTiedRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc() => OnGameTied?.Invoke(this, EventArgs.Empty);

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
        => OnGameWin?.Invoke(this, new OnGameWinEventArgs { line = lineList[lineIndex], winPlayerType = winPlayerType });

    // Server reset bảng → thông báo tất cả chơi lại
    [Rpc(SendTo.Server)]
    public void RematchRpc() {
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                playerTypeArray[x, y] = PlayerType.None;
        currentPlayablePlayerType.Value = PlayerType.Cross;
        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc() => OnRematch?.Invoke(this, EventArgs.Empty);

    public PlayerType GetLocalPlayerType()          => localPlayerType;
    public PlayerType GetCurrentPlayablePlayerType() => currentPlayablePlayerType.Value;
    public void GetScores(out int cross, out int circle) {
        cross  = playerCrossScore.Value;
        circle = playerCircleScore.Value;
    }
}
