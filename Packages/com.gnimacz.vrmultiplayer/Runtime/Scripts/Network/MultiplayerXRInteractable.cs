using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
[AddComponentMenu("Multiplayer/Network Interactable")]
[DisallowMultipleComponent]
public class MultiplayerXRInteractable : NetworkBehaviour
{
    private XRBaseInteractable grabInteractable;
    [SerializeField] private bool _isBeingHeld = false;
    private bool? initialKinematicState;

    [Tooltip("If true, anyone can pick this up after it's dropped. Otherwise, only the last owner can.")]
    public bool shouldUnlockWhenDropped = true;

    [Tooltip("If true, unparents this object from its parent when spawned.")]
    public bool unparentOnSpawn = false;

    private void Awake()
    {
        // Cache the initial kinematic state
        initialKinematicState ??= GetComponent<Rigidbody>().isKinematic;
        Debug.Log($"Initial Kinematic State for {gameObject.name}: {initialKinematicState.Value}");
    }

    private void Start()
    {
        grabInteractable = GetComponent<XRBaseInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("XRBaseInteractable component not found.");
            return;
        }

        if (IsServer)
        {
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }

        UpdateInteractionState();

        if (initialKinematicState.HasValue)
        {
            Debug.Log("Initial kinematic state: " + initialKinematicState.Value);
        }
    }

    private void OnEnable()
    {
        grabInteractable = GetComponent<XRBaseInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("XRBaseInteractable component not found.");
            return;
        }

        grabInteractable.firstSelectEntered.AddListener(TryPickup);
        grabInteractable.selectExited.AddListener(TryDrop);
    }

    private void OnDisable()
    {
        grabInteractable.firstSelectEntered.RemoveListener(TryPickup);
        grabInteractable.selectExited.RemoveListener(TryDrop);
    }

    protected override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();

        if (unparentOnSpawn && IsOwner)
        {
            NetworkObject.TryRemoveParent();
        }
    }
    
    public void TryPickup(SelectEnterEventArgs args)
    {
        if (!IsOwner && !_isBeingHeld)
        {
            RequestOwnershipServerRPC(NetworkManager.Singleton.LocalClientId);
        }

        if (IsOwner)
        {
            LockInteractionClientRPC(OwnerClientId, true);
        }
    }

    public void TryDrop(SelectExitEventArgs args)
    {
        if (IsOwner)
        {
            DropOwnershipServerRPC();

            // Restore initial kinematic state
            if (initialKinematicState.HasValue)
            {
                GetComponent<Rigidbody>().isKinematic = initialKinematicState.Value;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRPC(ulong clientId)
    {
        if (!_isBeingHeld)
        {
            NetworkObject.ChangeOwnership(clientId);
            LockInteractionClientRPC(clientId, true);
            _isBeingHeld = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DropOwnershipServerRPC()
    {
        if (shouldUnlockWhenDropped)
        {
            LockInteractionClientRPC(OwnerClientId, false);
        }

        _isBeingHeld = false;
    }

    public override void OnGainedOwnership()
    {
        UpdateInteractionState();
    }

    public override void OnLostOwnership()
    {
        UpdateInteractionState();
    }

    [ClientRpc]
    private void LockInteractionClientRPC(ulong ownerId, bool isLocked)
    {
        // Only the owner can interact if the object is locked
        if (NetworkManager.Singleton.LocalClientId != ownerId)
        {
            Debug.Log($"Locking interaction for client: {ownerId}, locked: {isLocked}");
            grabInteractable.enabled = !isLocked;
        }

        _isBeingHeld = isLocked;
    }

    private void UpdateInteractionState()
    {
        // If not held, enable for everyone; otherwise only the owner can interact
        grabInteractable.enabled = !_isBeingHeld || IsOwner;
    }
}
