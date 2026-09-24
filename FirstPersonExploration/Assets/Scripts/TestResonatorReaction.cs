using Game.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestResonatorReaction : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO channel;
    [SerializeField] private DissolveController _dissolveController;

    private void OnEnable()
    {
        if (channel != null)
        {
            channel.Register(HandleRaised);
        }
    }

    private void OnDisable()
    {
        if (channel != null)
        {
            channel.Unregister(HandleRaised);
        }
    }

    private void HandleRaised()
    {
        _dissolveController.Dissolve();
    }
}
