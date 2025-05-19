using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using VRMultiplayer.Editor;

public class NetworkManagerSpawner : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    static void SpawnNetWorkManagerIfRequired()
    {
        // Check if a NetworkManager already exists in the scene
        if (FindAnyObjectByType<NetworkManager>() == null)
        {
            Debug.Log("No NetworkManager found. Creating a new one.");


            // Instantiate the NetworkManager prefab
            GameObject networkManagerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Packages/com.gnimacz.vrmultiplayer/Runtime/prefabs/NetworkManager.prefab");
            networkManagerPrefab = Instantiate(networkManagerPrefab, Vector3.zero, Quaternion.identity);
            networkManagerPrefab.name = "NetworkManager";

            if (Settings.StartInSinglePlayer.Value)
            {
                // Set the NetworkManager to start in single-player mode
                NetworkManager networkManager = networkManagerPrefab.GetComponent<NetworkManager>();
                networkManager.StartHost();
            }
        }


        if (FindAnyObjectByType<LobbyManager>() == null)
        {
            // Create an instance of the LobbyManager
            GameObject LobbyManager = new() { name = "LobbyManager" };
            LobbyManager.AddComponent<LobbyManager>();
            LobbyManager.AddComponent<OnlineLobbyManager>();
        }
    }
}