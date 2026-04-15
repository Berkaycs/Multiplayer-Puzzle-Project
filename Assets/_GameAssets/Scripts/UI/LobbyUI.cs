using UnityEngine;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _joinCodeText;

    private void Start()
    {
        _joinCodeText.text = $"Join Code: {LobbyManager.Instance.GetJoinCode()}";
    }
}
