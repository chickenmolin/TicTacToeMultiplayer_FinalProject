using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// 1. Công dụng file:
// - Quản lý phần hiển thị (visual) của game Tic Tac Toe
// - Spawn X/O và line thắng trên board
// - Đồng bộ object qua network

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - GRID_SIZE: khoảng cách giữa các ô grid
// - crossPrefab: prefab quân X
// - circlePrefab: prefab quân O
// - lineCompletePrefab: prefab hiển thị đường thắng
// - visualGameObjectList: danh sách object đã spawn để quản lý/xóa

// b) Event từ GameManager:
// - OnClickedOnGridPosition:
//   + Spawn X/O tại vị trí grid
// - OnGameWin:
//   + Spawn line hiển thị đường thắng
// - OnRematch:
//   + Xóa toàn bộ object đã spawn

// c) Logic spawn:
// - SpawnObjectRpc(x, y, playerType):
//   + Xác định prefab (Cross / Circle)
//   + Instantiate object tại vị trí world
//   + Spawn qua NetworkObject

// d) Win visual:
// - GameManager_OnGameWin:
//   + Xác định hướng line (Horizontal / Vertical / Diagonal)
//   + Tính rotation tương ứng
//   + Spawn lineCompletePrefab tại vị trí center

// e) Rematch:
// - Xóa toàn bộ object trong visualGameObjectList
// - Clear danh sách

// f) Utility:
// - GetGridWorldPosition(x, y):
//   + Convert tọa độ grid → world position

// g) Network:
// - Sử dụng RPC để spawn object trên server
// - Dùng NetworkObject để sync giữa các client

// h) Dependencies:
// - GameManager: cung cấp event và dữ liệu gameplay
// - NetworkManager / Netcode: xử lý multiplayer
// - NetworkObject: đồng bộ object

public class GameVisualManager : NetworkBehaviour {
    private const float GRID_SIZE = 3.1f; // Khoảng cách giữa các ô

    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform lineCompletePrefab; // Đường gạch khi thắng

    private List<GameObject> visualGameObjectList; // Theo dõi object để xóa khi rematch

    private void Start() {
        GameManager.Instance.OnClickedOnGridPosition += GameManager_OnClickedOnGridPosition;
        GameManager.Instance.OnGameWin  += GameManager_OnGameWin;
        GameManager.Instance.OnRematch  += GameManager_OnRematch;
    }

    // Chỉ server xóa visual objects khi chơi lại
    private void GameManager_OnRematch(object sender, System.EventArgs e) {
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (GameObject obj in visualGameObjectList) Destroy(obj);
        visualGameObjectList.Clear();
    }

    // Chỉ server spawn đường thắng với góc xoay đúng hướng
    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e) {
        if (!NetworkManager.Singleton.IsServer) return;

        float eulerZ = e.line.orientation switch {
            GameManager.Orientation.Horizontal => 0f,
            GameManager.Orientation.Vertical   => 90f,
            GameManager.Orientation.DiagonalA  => 45f,
            GameManager.Orientation.DiagonalB  => -45f,
            _ => 0f
        };

        Transform line = Instantiate(lineCompletePrefab,
            GetGridWorldPosition(e.line.centerGridPosition.x, e.line.centerGridPosition.y),
            Quaternion.Euler(0, 0, eulerZ));
        line.GetComponent<NetworkObject>().Spawn(true); // Đồng bộ sang tất cả client
        visualGameObjectList.Add(line.gameObject);
    }

    // Khi có nước đi → gửi lên server để spawn X hoặc O
    private void GameManager_OnClickedOnGridPosition(object sender, GameManager.OnClickedOnGridPositionEventArgs e)
        => SpawnObjectRpc(e.x, e.y, e.playerType);

    [Rpc(SendTo.Server)]
    private void SpawnObjectRpc(int x, int y, GameManager.PlayerType playerType) {
        Transform prefab = playerType == GameManager.PlayerType.Circle ? circlePrefab : crossPrefab;
        Transform obj = Instantiate(prefab, GetGridWorldPosition(x, y), Quaternion.identity);
        obj.GetComponent<NetworkObject>().Spawn(true); // Đồng bộ sang tất cả client
        visualGameObjectList.Add(obj.gameObject);
    }

    // Chuyển tọa độ ô (0-2) sang world position
    private Vector2 GetGridWorldPosition(int x, int y)
        => new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
}
