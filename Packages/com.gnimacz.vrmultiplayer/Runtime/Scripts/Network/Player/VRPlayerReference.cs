using System;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// VRPlayerReference is a singleton class used to manage important references for VR player elements,
/// such as the root, head, left hand, and right hand in a VR environment.
/// These references are shared across the application to synchronize VR player states.
/// </summary>
public class VRPlayerReference : MonoBehaviour
{
    /// <summary>
    /// Gets the singleton instance of the VRPlayerReference class.
    /// </summary>
    /// <remarks>
    /// This property provides a globally accessible instance of the VRPlayerReference class.
    /// It is set internally and ensures only one instance exists during the application lifecycle.
    /// Used to grant access to VR player's root, head, left hand, and right hand transforms.
    /// </remarks>
    public static VRPlayerReference Instance { get; private set; }

    /// <summary>
    /// Represents the root transform of the VR player within a scene.
    /// This variable acts as the base or parent reference that encapsulates
    /// the VR player's entire structure, including associated components such as
    /// the Head, LeftHand, and RightHand.
    /// </summary>
    public Transform Root;

    /// <summary>
    /// Represents the transform of the VR player's head, typically used to track the position
    /// and rotation of the VR headset in the world space. This variable is expected to be set
    /// to the corresponding head transform within the VR rig.
    /// </summary>
    public Transform Head;

    /// <summary>
    /// Represents the transform of the player's left hand in the virtual reality environment.
    /// </summary>
    /// <remarks>
    /// This variable is typically updated to synchronize the position, rotation, and scale
    /// of the left hand in the VR environment. It is critical for accurate representation
    /// of the player's left-hand position and interaction in VR applications.
    /// </remarks>
    public Transform LeftHand;

    /// <summary>
    /// Represents the transform component for the player's right hand in a VR environment.
    /// This variable is used to synchronize the position, rotation, and scale of the VR player's right hand
    /// with its in-game representation.
    /// </summary>
    public Transform RightHand;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Ensures that there is only one instance of the VRPlayerReference class
    /// by implementing a Singleton pattern. If another instance already exists,
    /// the duplicate GameObject is destroyed.
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Instantiates a networked XR Origin prefab and positions it relative to the editor's camera.
    /// </summary>
    /// <remarks>
    /// The created prefab is specifically loaded from a predefined asset path within the project.
    /// This method is designed to be accessed through the Unity Editor's menu system.
    /// The prefab is initialized at a position offset from the editor camera's current location,
    /// to ensure it spawns in the visible scene space.
    /// </remarks>
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