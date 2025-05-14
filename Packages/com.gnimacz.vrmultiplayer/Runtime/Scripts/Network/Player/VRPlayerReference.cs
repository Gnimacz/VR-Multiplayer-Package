using System;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Serialization;

public class VRPlayerReference : MonoBehaviour
{
    public static VRPlayerReference Instance { get; private set; }

    public Transform Root;
    public Transform Head;
    public Transform LeftHand;
    public Transform RightHand;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [MenuItem("GameObject/XR/XR Origin (Networked)", false)]
    public static void CreatePlayerReference()
    {
        GameObject player = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Packages/com.gnimacz.vrmultiplayer/Runtime/prefabs/Modified XR Origin (XR Rig).prefab"));
        
        player.name = "Networked XR Origin";
        // Get the transform of the editor camera and move the object that way
        Transform editorCameraTransform = Camera.current? Camera.current.transform : player?.transform;
        player.transform.position = editorCameraTransform.position + editorCameraTransform.forward * 5;
    }
}