using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DoorSwitch : Door
{
    [SerializeField] private List<Switch> _switches;

    private Dictionary<Switch, bool> _activeSwitches = new Dictionary<Switch, bool>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (Switch doorSwitch in _switches)
            {
                doorSwitch.OnSwitchChanged += DoorSwitch_OnSwitchChanged;
                _activeSwitches.Add(doorSwitch, false);
            }
        }
    }

    private void DoorSwitch_OnSwitchChanged(Switch doorSwitch, bool isActive)
    {
        _activeSwitches[doorSwitch] = isActive;

        foreach (Switch doorswitch in _switches)
        {
            if (!_activeSwitches[doorswitch])
            {
                return;
            }
        }

        OpenDoorAnimation(14f, 3f);
    }
}
