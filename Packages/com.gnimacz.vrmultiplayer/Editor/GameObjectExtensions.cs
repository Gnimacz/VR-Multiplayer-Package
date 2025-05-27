using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRMultiplayer.Editor;
using VRMultiplayer.Network;

namespace gnimacz.vrmultiplayer.Editor
{
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
        public Type SelectedInteractableType;
    }

    [InitializeOnLoad]
    public static class NetworkedGameObjectEditor
    {
        private static readonly Dictionary<int, NetworkedGameObjectToggleState> _states = new();
        private static double _lastCleanupTime = 0;

        // Get a list of all types inheriting from XRBaseInteractable
        private static readonly List<Type> InteractableTypes =
            TypeCache.GetTypesDerivedFrom<XRBaseInteractable>().ToList();


        static NetworkedGameObjectEditor()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        private static void OnFinishedDefaultHeaderGUI(UnityEditor.Editor editor)
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
                    GUILayout.Toggle(state.HasNetworkTransform, "Sync Transform?", EditorStyles.radioButton);
                if (newTransformToggle != state.HasNetworkTransform)
                {
                    ToggleNetworkTransform(go, newTransformToggle);
                    state.HasNetworkTransform = newTransformToggle;
                    _states[id] = state;
                }

                bool newInteractableToggle =
                    GUILayout.Toggle(state.Interactable, "Is Object Interactable?", EditorStyles.radioButton);
                if (newInteractableToggle != state.Interactable)
                {
                    ToggleInteractable(go, newInteractableToggle);
                    state.Interactable = newInteractableToggle;
                    _states[id] = state;
                }

                if (state.HasNetworkTransform && go.GetComponent<Rigidbody>() &&
                    go.GetComponent<NetworkRigidbody>() == null)
                {
                    Undo.AddComponent<NetworkRigidbody>(go).UseRigidBodyForMotion = true;
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
                if (Settings.ShowDebugInformation.Value) Debug.Log($"Removed {typeof(T).Name}");
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

                // if (go.GetComponent<Rigidbody>())
                // {
                //     Undo.AddComponent<NetworkRigidbody>(go).UseRigidBodyForMotion = true;
                // }
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
                    EditorApplication.delayCall += () =>
                    {
                        GameObjectExtensionWindow.Show(_states[go.GetInstanceID()].SelectedInteractableType,
                            (type) =>
                            {
                                if (type != null)
                                {
                                    Undo.AddComponent(go, type);
                                    Undo.AddComponent<MultiplayerXRInteractable>(go);
                                    go.GetComponent<NetworkObject>().DontDestroyWithOwner = true;
                                }
                                else
                                {
                                    var state = _states[go.GetInstanceID()];
                                    state.Interactable = false;
                                    _states[go.GetInstanceID()] = state;
                                    EditorUtility.SetDirty(go);
                                    return;
                                }
                            });
                    };
                    return;
                }

                var interactable = Undo.AddComponent<MultiplayerXRInteractable>(go);
                go.GetComponent<NetworkObject>().DontDestroyWithOwner = true;
            }
            else
            {
                TryUndoDestroy<MultiplayerXRInteractable>(go);
            }

            EditorUtility.SetDirty(go);
        }
    }

    public class GameObjectExtensionWindow : EditorWindow
    {
        private static List<Type> _interactableTypes;
        private static Action<Type> _onSelected;

        private Vector2 _scroll;
        private Type _selectedType;

        public static void Show(Type currentSelection, Action<Type> onSelected)
        {
            _interactableTypes = TypeCache.GetTypesDerivedFrom<XRBaseInteractable>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToList();

            _onSelected = onSelected;

            var window = CreateInstance<GameObjectExtensionWindow>();
            window.titleContent = new GUIContent("Select Interactable Type");
            window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 400, 300);
            window._selectedType = currentSelection;
            window.ShowModalUtility(); // This blocks other editor input!
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("No interactable script was found on this object.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Please select one to add:", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var type in _interactableTypes)
            {
                bool isSelected = _selectedType == type;
                if (GUILayout.Toggle(isSelected, type.Name, "Button"))
                {
                    _selectedType = type;
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                _onSelected?.Invoke(null);
                Close();
            }

            GUI.enabled = _selectedType != null;
            if (GUILayout.Button("Select"))
            {
                _onSelected?.Invoke(_selectedType);
                Close();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}