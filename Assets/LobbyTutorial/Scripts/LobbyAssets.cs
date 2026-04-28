using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 1. Công dụng file:
// - Quản lý asset (Sprite) cho nhân vật trong Lobby
// - Cung cấp sprite tương ứng với từng loại nhân vật

// 2. Các mục quan trọng:

// a) Dữ liệu chính:
// - marineSprite: sprite nhân vật Marine
// - ninjaSprite: sprite nhân vật Ninja
// - zombieSprite: sprite nhân vật Zombie
// - Instance: singleton để truy cập toàn cục

// b) Logic chính:
// - GetSprite(playerCharacter):
//   + Nhận loại nhân vật
//   + Trả về sprite tương ứng

// c) Dependencies:
// - LobbyManager.PlayerCharacter: enum loại nhân vật
// - Sprite: Unity asset hiển thị hình ảnh

// d) Notes:
// - Sử dụng Singleton pattern
// - Dùng switch-case để map enum → sprite
// - Có default để tránh lỗi khi enum không hợp lệ

// Singleton quản lý sprite cho các nhân vật trong Lobby

public class LobbyAssets : MonoBehaviour {
    public static LobbyAssets Instance { get; private set; }

    // Sprite 3 nhân vật, gán trong Inspector
    [SerializeField] private Sprite marineSprite;
    [SerializeField] private Sprite ninjaSprite;
    [SerializeField] private Sprite zombieSprite;

    private void Awake() { Instance = this; }

    // Trả về sprite tương ứng với nhân vật được chọn
    public Sprite GetSprite(LobbyManager.PlayerCharacter playerCharacter) {
        switch (playerCharacter) {
            default:
            case LobbyManager.PlayerCharacter.Marine:  return marineSprite;
            case LobbyManager.PlayerCharacter.Ninja:   return ninjaSprite;
            case LobbyManager.PlayerCharacter.Zombie:  return zombieSprite;
        }
    }
}
