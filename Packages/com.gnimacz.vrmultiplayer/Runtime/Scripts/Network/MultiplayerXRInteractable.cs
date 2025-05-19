using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRMultiplayer.Editor;

[RequireComponent(typeof(XRBaseInteractable))]
[AddComponentMenu("Multiplayer/Network Interactable")]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-10)]
public class MultiplayerXRInteractable : NetworkBehaviour
{
    private XRBaseInteractable grabInteractable;

    [NonSerialized] public NetworkVariable<bool> isBeingHeld =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool? initialKinematicState;

    [Tooltip("If true, anyone can pick this up after it's dropped. Otherwise, only the last owner can.")]
    public bool shouldUnlockWhenDropped = true;

    [Tooltip("If true, unparents this object from its parent when spawned.")]
    public bool unparentOnSpawn = false;

    private void Awake()
    {
        // Cache the initial kinematic state
        initialKinematicState ??= GetComponent<Rigidbody>().isKinematic;
#if UNITY_EDITOR
        if (Settings.ShowDebugInformation.Value)
            Debug.Log($"Initial Kinematic State for {gameObject.name}: {initialKinematicState.Value}");
#endif
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

        if (initialKinematicState.HasValue)
        {
#if UNITY_EDITOR
            if (Settings.ShowDebugInformation.Value)
                Debug.Log("Initial kinematic state: " + initialKinematicState.Value);
#endif
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

        isBeingHeld.OnValueChanged += OnIsBeingHeldChanged;
    }

    private void OnDisable()
    {
        grabInteractable.firstSelectEntered.RemoveListener(TryPickup);
        grabInteractable.selectExited.RemoveListener(TryDrop);
        isBeingHeld.OnValueChanged -= OnIsBeingHeldChanged;
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
#if UNITY_EDITOR
        if (Settings.ShowDebugInformation.Value)
            Debug.Log($"[{gameObject.name}] Attempting pickup. IsOwner: {IsOwner}, IsBeingHeld: {isBeingHeld.Value}");
#endif
        if (!IsOwner && !isBeingHeld.Value)
        {
            RequestOwnershipServerRPC(NetworkManager.Singleton.LocalClientId);
            StartCoroutine(WaitToBecomeOwner());
        }

        if (IsOwner)
        {
            isBeingHeld.Value = true;
        }
    }

    private IEnumerator WaitToBecomeOwner()
    {
        yield return new WaitUntil(() => IsOwner);

        isBeingHeld.Value = true;
    }

    public void TryDrop(SelectExitEventArgs args)
    {
        if (IsOwner)
        {
            // Restore initial kinematic state
            if (initialKinematicState.HasValue)
            {
                GetComponent<Rigidbody>().isKinematic = initialKinematicState.Value;
            }

            isBeingHeld.Value = false;
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestOwnershipServerRPC(ulong clientId)
    {
        if (!isBeingHeld.Value)
        {
            NetworkObject.ChangeOwnership(clientId);
        }
    }

    private void OnIsBeingHeldChanged(bool previousValue, bool newValue)
    {
        EnableOrDisableGrabInteractable(newValue);
    }

    private void EnableOrDisableGrabInteractable(bool enable)
    {
#if UNITY_EDITOR
        if (Settings.ShowDebugInformation.Value) Debug.Log($"Setting grab interactable enabled to: {!enable}");
#endif
        if (grabInteractable != null && !IsOwner)
        {
            grabInteractable.enabled = !enable;
        }
    }
}