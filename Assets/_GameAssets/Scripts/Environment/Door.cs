using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Door : NetworkBehaviour
{
    protected void OpenDoorAnimation(float animTo, float duration)
    {
        transform.DOLocalMoveY(animTo, duration, false).SetEase(Ease.Linear);
    }
}
