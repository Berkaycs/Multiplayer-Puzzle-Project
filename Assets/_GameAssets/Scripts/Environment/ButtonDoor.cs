using System;
using Unity.Netcode;
using UnityEngine;

public class ButtonDoor : NetworkBehaviour
{
    public event Action<ButtonDoor> OnButtonPressed;

    public void Activate()
    {
        if (IsServer)
        {
            OnButtonPressed?.Invoke(this);
            Debug.Log("Button is activated");
        }
    }
}
