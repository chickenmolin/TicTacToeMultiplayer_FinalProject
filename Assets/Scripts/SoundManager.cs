using UnityEngine;

public class SoundManager : MonoBehaviour {
    [SerializeField] private Transform placeSfxPrefab; // Âm thanh đặt quân
    [SerializeField] private Transform winSfxPrefab;   // Âm thanh thắng
    [SerializeField] private Transform loseSfxPrefab;  // Âm thanh thua

    private void Start() {
        GameManager.Instance.OnPlacedObject += GameManager_OnPlacedObject;
        GameManager.Instance.OnGameWin      += GameManager_OnGameWin;
    }

    // Phát âm thắng hoặc thua tùy người chơi local
    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e) {
        bool isLocalWin = GameManager.Instance.GetLocalPlayerType() == e.winPlayerType;
        Transform sfx = Instantiate(isLocalWin ? winSfxPrefab : loseSfxPrefab);
        Destroy(sfx.gameObject, 5f); // Tự xóa sau 5s
    }

    // Phát âm khi đặt quân
    private void GameManager_OnPlacedObject(object sender, System.EventArgs e) {
        Transform sfx = Instantiate(placeSfxPrefab);
        Destroy(sfx.gameObject, 5f);
    }
}
