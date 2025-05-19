using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class GameObjectExtensions
{
    public static bool IsNetworked(this GameObject go)
        => go.TryGetComponent<NetworkObject>(out _);

    public static bool HasNetworkManager(this GameObject go)
        => go.TryGetComponent<NetworkManager>(out _);
}

struct NetworkedGameObjectToggleState
{
    public bool IsNetworked;
    public bool HasNetworkTransform;
    public bool Interactable;
}

[InitializeOnLoad]
public static class NetworkedGameObjectEditor
{
    private static readonly Dictionary<int, NetworkedGameObjectToggleState> _states = new();
    private static double _lastCleanupTime = 0;

    static NetworkedGameObjectEditor()
    {
        Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
    }

    private static void OnFinishedDefaultHeaderGUI(Editor editor)
    {
        if (editor.target is not GameObject go || go.HasNetworkManager()) return;

        CleanupStaleStatesIfNeeded();

        int id = go.GetInstanceID();
        if (!_states.TryGetValue(id, out var state))
        {
            state = new NetworkedGameObjectToggleState
            {
                IsNetworked = go.IsNetworked(),
                HasNetworkTransform = go.GetComponent<NetworkTransform>() != null,
                Interactable = go.GetComponent<MultiplayerXRInteractable>() != null
            };
            _states[id] = state;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Networked GameObject", EditorStyles.boldLabel);

        if (state.IsNetworked)
        {
            EditorGUILayout.LabelField("Status", "Networked", EditorStyles.whiteLargeLabel);

            if (GUILayout.Button("Remove Networking Components"))
            {
                TryUndoDestroy<MultiplayerXRInteractable>(go);
                TryUndoDestroy<NetworkRigidbody>(go);
                TryUndoDestroy<NetworkTransform>(go);
                TryUndoDestroy<NetworkObject>(go);

                state.IsNetworked = false;
                _states[id] = state;

                EditorUtility.SetDirty(go);
                return;
            }

            bool newTransformToggle =
                GUILayout.Toggle(state.HasNetworkTransform, "Sync Transform?", EditorStyles.miniButtonMid);
            if (newTransformToggle != state.HasNetworkTransform)
            {
                ToggleNetworkTransform(go, newTransformToggle);
                state.HasNetworkTransform = newTransformToggle;
                _states[id] = state;
            }

            bool newInteractableToggle =
                GUILayout.Toggle(state.Interactable, "Is Object Interactable?", EditorStyles.miniButtonMid);
            if (newInteractableToggle != state.Interactable)
            {
                ToggleInteractable(go, newInteractableToggle);
                state.Interactable = newInteractableToggle;
                _states[id] = state;
            }

            var networkObject = go.GetComponent<NetworkObject>();
            bool shouldDestroySelf = networkObject.DontDestroyWithOwner;
            bool newShouldDestroySelf = GUILayout.Toggle(!shouldDestroySelf,
                "Destroy this object when the owner is destroyed?");
            networkObject.DontDestroyWithOwner = !newShouldDestroySelf;
        }
        else

        {
            EditorGUILayout.LabelField("Status", "Not Networked", EditorStyles.whiteLargeLabel);

            if (GUILayout.Button("Add NetworkObject"))
            {
                Undo.AddComponent<NetworkObject>(go);
                ToggleNetworkTransform(go, state.HasNetworkTransform);
                ToggleInteractable(go, state.Interactable);
                state.IsNetworked = true;
                _states[id] = state;

                EditorUtility.SetDirty(go);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static void CleanupStaleStatesIfNeeded()
    {
        if (EditorApplication.timeSinceStartup - _lastCleanupTime < 10) return;

        _lastCleanupTime = EditorApplication.timeSinceStartup;

        var staleKeys = new List<int>();
        foreach (var kvp in _states)
        {
            if (EditorUtility.InstanceIDToObject(kvp.Key) == null)
                staleKeys.Add(kvp.Key);
        }

        foreach (var key in staleKeys)
            _states.Remove(key);
    }

    private static void TryUndoDestroy<T>(GameObject go) where T : Component
    {
        if (go.TryGetComponent<T>(out var component))
        {
            Undo.DestroyObjectImmediate(component);
            Debug.Log($"Removed {typeof(T).Name}");
        }
    }

    private static void ToggleNetworkTransform(GameObject go, bool enable)
    {
        if (enable)
        {
            var netTransform = Undo.AddComponent<NetworkTransform>(go);
            netTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            netTransform.UseQuaternionSynchronization = true;
            netTransform.UseQuaternionCompression = true;

            if (go.GetComponent<Rigidbody>())
            {
                Undo.AddComponent<NetworkRigidbody>(go).UseRigidBodyForMotion = true;
            }

            Debug.Log("Added NetworkTransform (and optionally NetworkRigidbody)");
        }
        else
        {
            TryUndoDestroy<NetworkRigidbody>(go);
            TryUndoDestroy<NetworkTransform>(go);
        }

        EditorUtility.SetDirty(go);
    }

    private static void ToggleInteractable(GameObject go, bool enable)
    {
        if (enable)
        {
            if (!go.GetComponent<XRBaseInteractable>())
            {
                Undo.AddComponent<XRSimpleInteractable>(go);
                Debug.LogWarning(
                    "Added XRSimpleInteractable. This is a simple interactable that does not require a collider. If you need a collider, please add it manually.");
            }

            var interactable = Undo.AddComponent<MultiplayerXRInteractable>(go);
            go.GetComponent<NetworkObject>().DontDestroyWithOwner = true;
            Debug.Log("Added MultiplayerXRInteractable");
        }
        else
        {
            TryUndoDestroy<MultiplayerXRInteractable>(go);
        }

        EditorUtility.SetDirty(go);
    }
}