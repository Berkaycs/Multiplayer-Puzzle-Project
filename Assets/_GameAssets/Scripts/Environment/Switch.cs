using System;
using Unity.Netcode;
using UnityEngine;

public class Switch : NetworkBehaviour
{
    public event Action<Switch, bool> OnSwitchChanged;

    private NetworkVariable<bool> _isActive = new NetworkVariable<bool>();

    public override void OnNetworkSpawn()
    {
        _isActive.OnValueChanged += IsActive_OnValueChanged;
    }

    private void IsActive_OnValueChanged(bool wasActive, bool isActive)
    {
        if (isActive) Debug.Log("IsActive");

        else Debug.Log("IsNotActive");
    }

    [Rpc(SendTo.Server)]
    private void OnSwitchChangedServerRpc(bool isActive)
    {
        _isActive.Value = isActive;
        OnSwitchChanged?.Invoke(this, isActive);
    }

    private void OnTriggerEnter(Collider other)
    {
        OnSwitchChangedServerRpc(true);
    }

    private void OnTriggerExit(Collider other)
    {
        OnSwitchChangedServerRpc(false);
    }
}
