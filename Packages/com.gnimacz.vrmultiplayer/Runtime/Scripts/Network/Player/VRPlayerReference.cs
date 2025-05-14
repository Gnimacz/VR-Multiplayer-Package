using System;
using Unity.XR.CoreUtils;
using UnityEditor;
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
    
    [MenuItem("GameObject/VR Multiplayer/Create Player Reference", false, 0)]
    public static void CreatePlayerReference()
    {
        Debug.Log("Creating Player Reference...");
    }
}