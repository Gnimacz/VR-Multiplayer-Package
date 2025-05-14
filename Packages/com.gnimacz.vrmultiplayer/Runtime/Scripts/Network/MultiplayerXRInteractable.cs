using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Provides a multiplayer-compatible XR interactable component for use with Unity's XR Interaction Toolkit and Netcode for GameObjects.
/// </summary>
/// <remarks>
/// This class allows XR interactables to function in a networked multiplayer environment by managing ownership,
/// locking interactions to specific clients, and synchronizing interactions across the network.
/// The component interacts with Unity's XRBaseInteractable for local interaction behavior and extends it to
/// support multiplayer scenarios through Netcode for GameObjects functionalities.
/// </remarks>
[RequireComponent(typeof(XRBaseInteractable))]
[AddComponentMenu("Multiplayer/Network Interactable")]
[DisallowMultipleComponent]
public class MultiplayerXRInteractable : NetworkBehaviour
{
    /// <summary>
    /// Represents an instance of the XRBaseInteractable component used to handle interaction events
    /// in multi-user environments for the MultiplayerXRInteractable class.
    /// </summary>
    /// <remarks>
    /// This variable is initialized using the XRBaseInteractable component attached to the same
    /// GameObject. It is utilized to manage select interaction events such as pickups and drops,
    /// and controls interaction accessibility based on ownership and locking state.
    /// </remarks>
    private XRBaseInteractable grabInteractable;

    /// <summary>
    /// Represents whether the interactable object is currently being held by a player.
    /// </summary>
    /// <remarks>
    /// This variable is used to control and synchronize the interaction state of the object
    /// across the network in a multiplayer environment. It updates when the object is picked up
    /// or dropped and is crucial for locking or unlocking interactions based on ownership and
    /// game logic.
    /// </remarks>
    [SerializeField] private bool _isBeingHeld = false;

    /// <summary>
    /// Represents the initial kinematic state of the Rigidbody component
    /// associated with the object. This value is cached during the Awake phase
    /// to retain the object's original physical behavior and can be used to
    /// restore it upon releasing ownership or resetting the object state.
    /// </summary>
    private bool? initialKinematicState;

    /// <summary>
    /// Determines if the object should be unlocked for interaction by other players
    /// after it is dropped. If set to true, the object becomes interactable by any player
    /// upon being dropped. If set to false, only the last owner can interact with the object
    /// after it is dropped.
    /// </summary>
    [Tooltip("If true, anyone can pick this up after it's dropped. Otherwise, only the last owner can.")]
    public bool shouldUnlockWhenDropped = true;

    /// <summary>
    /// Determines whether the object will be unparented from its current parent transform
    /// upon being spawned in the network. If set to true, the object will detach
    /// from any hierarchical parent when it is instantiated across the network.
    /// </summary>
    [Tooltip("If true, unparents this object from its parent when spawned.")]
    public bool unparentOnSpawn = false;

    /// <summary>
    /// Called when the object is instantiated or enabled.
    /// Sets up initial states and configurations required by the interactable object, such as caching the initial
    /// kinematic state of the attached Rigidbody.
    /// Logs the initial kinematic state of the object for debugging purposes.
    /// </summary>
    private void Awake()
    {
        // Cache the initial kinematic state
        initialKinematicState ??= GetComponent<Rigidbody>().isKinematic;
        Debug.Log($"Initial Kinematic State for {gameObject.name}: {initialKinematicState.Value}");
    }

    /// <summary>
    /// Unity method called when the script instance is being loaded.
    /// This method initializes necessary components and assigns ownership of the NetworkObject
    /// to the server if the script is running on the server. Additionally, it updates the
    /// interaction state and logs the initial kinematic state of the attached Rigidbody, if available.
    /// </summary>
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

    /// <summary>
    /// Initializes the interactable object's behavior when the component is enabled.
    /// This method sets up event listeners for the interactable's select entered and exited events.
    /// If the required XRBaseInteractable component is not found, an error will be logged.
    /// </summary>
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

    /// <summary>
    /// Called when the GameObject or Component this script is attached to is disabled.
    /// Cleans up event listeners attached to the XRBaseInteractable component,
    /// ensuring proper disposal and avoiding potential memory leaks.
    /// Specifically, removes listeners for interaction events related to object selection
    /// such as picking up or dropping an object.
    /// </summary>
    private void OnDisable()
    {
        grabInteractable.firstSelectEntered.RemoveListener(TryPickup);
        grabInteractable.selectExited.RemoveListener(TryDrop);
    }

    /// <summary>
    /// Called automatically after the network spawn process is completed for the object.
    /// This method can be overridden to define custom behavior that should occur
    /// right after the object is spawned on the network.
    /// </summary>
    /// <remarks>
    /// If <c>unparentOnSpawn</c> is enabled and the calling client is the object owner,
    /// the object will be unparented from its current parent in the hierarchy.
    /// </remarks>
    protected override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();

        if (unparentOnSpawn && IsOwner)
        {
            NetworkObject.TryRemoveParent();
        }
    }

    /// <summary>
    /// Attempts to pick up the interactable object, setting ownership and locking interaction if necessary.
    /// </summary>
    /// <param name="args">The arguments related to the select enter event.</param>
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

    /// <summary>
    /// Attempts to drop the interactable object in the context of multiplayer.
    /// This method restores the initial kinematic state of the object's Rigidbody
    /// and handles releasing ownership of the object to the server.
    /// </summary>
    /// <param name="args">The event arguments containing context for the select exit interaction, triggered when the object is released.</param>
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

    /// <summary>
    /// Requests ownership of the networked object for a specific client.
    /// </summary>
    /// <param name="clientId">The ID of the client requesting ownership.</param>
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

    /// <summary>
    /// Allows the current owner to release ownership of the networked object.
    /// </summary>
    /// <remarks>
    /// This method is executed on the server through a ServerRPC call. If the
    /// <c>shouldUnlockWhenDropped</c> property is set to true, it unlocks the
    /// object for interaction by other clients after ownership has been released.
    /// It also updates the interaction lock state for all clients and marks the
    /// object as no longer being held.
    /// </remarks>
    [ServerRpc(RequireOwnership = false)]
    private void DropOwnershipServerRPC()
    {
        if (shouldUnlockWhenDropped)
        {
            LockInteractionClientRPC(OwnerClientId, false);
        }

        _isBeingHeld = false;
    }

    /// <summary>
    /// Called when this object gains network ownership.
    /// This method is triggered whenever the local client becomes the owner of the NetworkObject in a multiplayer context.
    /// It is responsible for updating the interaction state of the object
    /// to reflect ownership-related changes and ensure proper synchronization of interaction behavior.
    /// </summary>
    public override void OnGainedOwnership()
    {
        UpdateInteractionState();
    }

    /// <summary>
    /// Called when this NetworkObject loses ownership on the server.
    /// Allows handling necessary state changes or updates when the ownership
    /// of the object transitions away from the current client.
    /// This method is invoked automatically and can be used to reset interaction
    /// states, update locking mechanisms, or notify other components about
    /// the ownership loss.
    /// </summary>
    public override void OnLostOwnership()
    {
        UpdateInteractionState();
    }

    /// <summary>
    /// Locks or unlocks interaction with the object for a specific client.
    /// </summary>
    /// <param name="ownerId">The ID of the client that owns the object.</param>
    /// <param name="isLocked">Indicates whether the interaction should be locked or unlocked for other clients.</param>
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

    /// <summary>
    /// Updates the interaction state of the object, enabling or disabling interactions
    /// based on the object's ownership and whether it is currently being held.
    /// </summary>
    /// <remarks>
    /// This method ensures that the object is interactable only when appropriate.
    /// If the object is not being held, the interaction is enabled for all users.
    /// Otherwise, only the owner can interact with the object.
    /// </remarks>
    private void UpdateInteractionState()
    {
        // If not held, enable for everyone; otherwise only the owner can interact
        grabInteractable.enabled = !_isBeingHeld || IsOwner;
    }
}
