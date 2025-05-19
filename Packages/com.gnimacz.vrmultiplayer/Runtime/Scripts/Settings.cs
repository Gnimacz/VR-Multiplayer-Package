using System;
using UnityEditor;
using UnityEngine;

namespace VRMultiplayer.Editor
{
    public class BoolPreference
    {
        public string key { get; private set; }
        public bool defaultValue { get; private set; }

        public BoolPreference(string key, bool defaultValue)
        {
            this.key = key;
            this.defaultValue = defaultValue;
        }

        private bool? valueCache = null;

        public bool Value
        {
            get
            {
                if (valueCache == null)
                    valueCache = EditorPrefs.GetBool(key, defaultValue);

                return (bool)valueCache;
            }
            set
            {
                if (valueCache == value)
                    return;

                EditorPrefs.SetBool(key, value);
                valueCache = value;
                Debug.Log("Editor preference updated. key: " + key + ", value: " + value);
            }
        }

        public void ClearValue()
        {
            EditorPrefs.DeleteKey(key);
            valueCache = null;
        }
    }
}

namespace VRMultiplayer.Editor
{
    public class Settings : EditorWindow
    {
        public static BoolPreference UseOnlineLobby =
            new("UseOnlineLobby", false);

        public static BoolPreference StartInSinglePlayer =
            new("StartInSinglePlayer", false);

        public static BoolPreference ShowDebugInformation =
            new("ShowDebugInformation", false);

        [MenuItem("Multiplayer/Settings", priority = 1)]
        private static void InitWindow()
        {
            Settings window = (Settings)EditorWindow.GetWindow(typeof(Settings));
            window.titleContent = new GUIContent("Multiplayer Settings");
            window.minSize = new Vector2(550, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Multiplayer Settings", EditorStyles.boldLabel);
            UseOnlineLobby.Value = EditorGUILayout.Toggle("Use Online Lobby", UseOnlineLobby.Value);
            StartInSinglePlayer.Value = EditorGUILayout.Toggle("Start in Single Player", StartInSinglePlayer.Value);
            ShowDebugInformation.Value = EditorGUILayout.Toggle("Show Debug Information", ShowDebugInformation.Value);

            if (GUILayout.Button("Close"))
            {
                Close();
            }
        }
    }
}