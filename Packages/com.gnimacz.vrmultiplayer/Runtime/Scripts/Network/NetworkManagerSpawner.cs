using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class NetworkManagerSpawner : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    static void SpawnNetWorkManagerIfRequired()
    {
        // Check if a NetworkManager already exists in the scene
        if (FindAnyObjectByType<NetworkManager>() != null)
        {
            Debug.Log("NetworkManager already exists in the scene.");
            return;
        }

        // Instantiate the NetworkManager prefab
        GameObject networkManagerPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Packages/com.gnimacz.vrmultiplayer/Runtime/prefabs/NetworkManager.prefab");
        networkManagerPrefab = Instantiate(networkManagerPrefab, Vector3.zero, Quaternion.identity);
        networkManagerPrefab.name = "NetworkManager";
    }
}