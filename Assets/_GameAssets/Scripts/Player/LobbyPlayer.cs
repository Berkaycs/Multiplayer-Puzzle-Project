using TMPro;
using UnityEngine;

public class LobbyPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text _playerName;
    [SerializeField] private Renderer _isReadyRenderer;

    private LobbyPlayerData _data;
    private MaterialPropertyBlock _materialPropertyBlock;

    private void Start()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
    }

    public void SetData(LobbyPlayerData data)
    {
        _data = data;
        _playerName.text = _data.GamerTag;

        if (_data.IsReady && _isReadyRenderer != null)
        {
            _isReadyRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor("_BaseColor", Color.green);
            _isReadyRenderer.SetPropertyBlock(_materialPropertyBlock);

        }

        gameObject.SetActive(true);
    }
}
