// Detects the player entering and leaving this trigger volume and raises plain C# events
// for sibling components on the same GameObject, such as MusicZone and AmbienceZone.
// A kinematic Rigidbody is required because trigger events need a Rigidbody on at least one side.
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public sealed class PlayerTriggerZone : MonoBehaviour
{
    [SerializeField] private string _playerTag = "Player";

    private int _overlapCount;

    public event Action Entered;
    public event Action Exited;

    public bool IsPlayerInside => _overlapCount > 0;

    private void Reset()
    {
        ConfigureBody();
    }

    private void Awake()
    {
        ConfigureBody();

        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider == null)
        {
            Debug.LogWarning("PlayerTriggerZone: no Collider found. Add a collider with Is Trigger enabled.", this);
        }
        else if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning("PlayerTriggerZone: the collider is not set to Is Trigger.", this);
        }
    }

    private void OnDisable()
    {
        _overlapCount = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_playerTag))
        {
            return;
        }

        _overlapCount++;
        if (_overlapCount == 1)
        {
            Entered?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_overlapCount == 0 || !other.CompareTag(_playerTag))
        {
            return;
        }

        _overlapCount--;
        if (_overlapCount == 0)
        {
            Exited?.Invoke();
        }
    }

    private void ConfigureBody()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            return;
        }

        body.isKinematic = true;
        body.useGravity = false;
    }
}
