using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DoorButton : Door
{
    [SerializeField] private List<ButtonDoor> _buttons;
    [SerializeField] private float _timeBetweenButtonPressed;

    private Dictionary<ButtonDoor, bool> _activeButtons = new Dictionary<ButtonDoor, bool>();
    private float _lastButtonPressed = 0;

    public override void OnNetworkSpawn()
    {
        foreach (ButtonDoor buttonDoor in _buttons)
        {
            buttonDoor.OnButtonPressed += ButtonDoor_OnButtonPressed;
            _activeButtons.Add(buttonDoor, false);
        }
    }

    private void ButtonDoor_OnButtonPressed(ButtonDoor buttonDoor)
    {
        if (_buttons.Contains(buttonDoor))
        {
            _activeButtons[buttonDoor] = true;
            int numberOfButtonPressed = CountActivatedButtons();

            if (numberOfButtonPressed == 1)
            {
                _lastButtonPressed = Time.time;

                if (numberOfButtonPressed == _buttons.Count)
                {
                    OpenDoorAnimation(14f ,3f);
                }
            }
            else
            {
                if (_lastButtonPressed + _timeBetweenButtonPressed >= Time.time)
                {
                    if (numberOfButtonPressed == _buttons.Count)
                    {
                        OpenDoorAnimation(14f, 3f);
                    }
                }
                else
                {
                    ResetButtons();
                    _activeButtons[buttonDoor] = true;
                    _lastButtonPressed = Time.time;
                }
            }
        }
    }

    private int CountActivatedButtons()
    {
        int numberOfButtonPressed = 0;

        foreach (KeyValuePair<ButtonDoor, bool> button in _activeButtons)
        {
            numberOfButtonPressed = button.Value ? numberOfButtonPressed + 1 : numberOfButtonPressed;
        }

        return numberOfButtonPressed;
    }

    private void ResetButtons()
    {
        foreach (ButtonDoor button in _buttons)
        {
            _activeButtons[button] = false;
        }
    }
}
