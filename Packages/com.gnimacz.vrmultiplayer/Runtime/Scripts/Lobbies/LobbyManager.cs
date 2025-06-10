using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using gnimacz.vrmultiplayer.Lobbies.Local_Lobby;
using gnimacz.vrmultiplayer.Lobbies.Online_Lobby;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using VRMultiplayer.Editor;

namespace gnimacz.vrmultiplayer.Lobbies
{
    /// <summary>
    /// Represents the data required for creating a lobby.
    /// The struct holds information about a locally created lobby or an online lobby.
    /// </summary>
    [Serializable]
    public struct LobbyCreationData
    {
        /// <summary>
        /// Represents the generated room code for a local lobby.
        /// This code is used to uniquely identify and connect to a locally-hosted lobby.
        /// </summary>
        [CanBeNull] public string LocalRoomCode;

        /// Represents the currently active online lobby in the system.
        /// This variable is part of the LobbyCreationData struct and is used
        /// to store and access information about a created or joined lobby
        /// for online multiplayer functionality.
        [CanBeNull] public Lobby OnlineLobby;
    }

    /// <summary>
    /// Manages the creation, joining, and manipulation of game lobbies.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        /// <summary>
        /// Provides a static reference to the singleton instance of the LobbyManager class.
        /// This instance is automatically assigned when the LobbyManager object is created in the scene.
        /// Only one instance of LobbyManager can exist at a time. If multiple instances are detected,
        /// the additional instances are destroyed to enforce the singleton pattern.
        /// </summary>
        public static LobbyManager Instance { get; private set; }

        /// <summary>
        /// Represents an online lobby instance managed through Unity Services Lobbies.
        /// OnlineLobby is used for creating, joining, and managing multiplayer game lobbies
        /// with functionality such as setting lobby name, password, and player limits.
        /// </summary>
        private OnlineLobbyManager OnlineLobby => GetComponent<OnlineLobbyManager>();

        /// <summary>
        /// Determines whether the online lobby functionality should be used.
        /// </summary>
        /// <remarks>
        /// This property checks the value of the `UseOnlineLobby` setting from the application's settings configuration.
        /// When enabled, networked lobby functionality is used. If disabled, the application operates in a local or offline mode
        /// for lobby-related operations such as creation, joining, or managing lobbies.
        /// </remarks>
        private bool UseOnline => Settings.UseOnlineLobby.Value;

        public LobbyCreationData LobbyCreationData { get; private set; }

        /// <summary>
        /// Initializes the LobbyManager instance and ensures it persists across scenes.
        /// </summary>
        /// <remarks>
        /// If an instance of LobbyManager already exists, the duplicate object is destroyed.
        /// This method assigns the instance to the static property <see cref="Instance"/> and marks the object
        /// to not be destroyed when a new scene is loaded.
        /// </remarks>
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// This method is called when the LobbyManager is initialized and started.
        /// It performs setup logic necessary for the initialization of the LobbyManager, such as verifying the settings
        /// and logging debug information in the Unity Editor.
        private void Start()
        {
#if UNITY_EDITOR
            if (Settings.ShowDebugInformation.Value)
                Debug.Log($"LobbyManager started. Using Online Lobby?: {UseOnline}");
#endif
            if (OnlineLobby == null)
                gameObject.AddComponent<OnlineLobbyManager>();
        }

        /// Creates a new lobby based on the current settings.
        /// If the online mode is enabled, an online lobby will be created.
        /// Otherwise, a local lobby will be initialized.
        /// <returns>
        /// A Task that resolves with the creation data of the lobby.
        /// The returned object contains information about the created lobby, such as the room code for local lobbies or the lobby object for online lobbies.
        /// </returns>
        public async Task<LobbyCreationData> CreateLobby()
        {
            LobbyCreationData = UseOnline ? await CreateOnlineLobby() : await CreateLocalLobby();
            return LobbyCreationData;
        }

        /// <summary>
        /// Joins a lobby based on the provided room ID and optional password.
        /// </summary>
        /// <param name="roomID">The unique identifier of the lobby room to join.</param>
        /// <param name="password">The optional password required to join the lobby. Defaults to an empty string.</param>
        public void JoinLobby(string roomID, string password = "")
        {
            if (UseOnline)
                JoinOnlineLobby(roomID, password);
            else
                JoinLocalLobby(roomID);
        }

        /// Joins a local multiplayer lobby using the provided room code.
        /// <param name="roomCode">
        /// The unique identifier for the local lobby to join.
        /// </param>
        private async void JoinLocalLobby(string roomCode)
        {
#if UNITY_EDITOR
            if (Settings.ShowDebugInformation.Value)
                Debug.Log($"Joining local lobby with room code: {roomCode}");
#endif
            var (ip, port) = LocalLobby.DecodeRoomCode(roomCode);

            NetworkManager.Singleton.Shutdown();
            await Task.Delay(10);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, (ushort)port);
            NetworkManager.Singleton.StartClient();
        }

        /// <summary>
        /// Joins an online lobby using the specified lobby ID and optional password.
        /// </summary>
        /// <param name="lobbyID">The unique identifier of the lobby to join.</param>
        /// <param name="password">The password for the lobby. If null or empty, no password will be used.</param>
        private async void JoinOnlineLobby(string lobbyID, string password = "")
        {
            if (!string.IsNullOrEmpty(password))
            {
                OnlineLobby.SetLobbyPassword(password);
            }

            await OnlineLobby.JoinRelayAsync(lobbyID);
        }

        /// Finds and retrieves a list of online lobbies available in the lobby service.
        /// <returns>A task representing the asynchronous operation. The task contains a list of online lobbies available.</returns>
        public async Task<List<Lobby>> FindOnlineLobbies()
        {
            return await OnlineLobby.GetAllLobbiesAsync();
        }

        /// <summary>
        /// Creates a local lobby by initializing local networking components and generating a unique room code.
        /// </summary>
        /// <returns>
        /// A <see cref="LobbyCreationData"/> object containing the generated room code
        /// for the local lobby.
        /// </returns>
        private async Task<LobbyCreationData> CreateLocalLobby()
        {
            NetworkManager.Singleton.Shutdown();
            await Task.Delay(100);
            string roomCode = LocalLobby.Create();
#if UNITY_EDITOR
            if (Settings.ShowDebugInformation.Value)
                Debug.Log($"Roomcode: {roomCode}");
#endif
            var otherInfo = LocalLobby.DecodeRoomCode(roomCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetConnectionData(otherInfo.Item1, (ushort)otherInfo.Item2);
            NetworkManager.Singleton.StartHost();
            return new LobbyCreationData { LocalRoomCode = roomCode };
        }

        /// Creates an online lobby by starting a host relay session and establishing a network connection.
        /// This method shuts down the existing network manager, initializes the host relay session using
        /// Unity Services, configures the network transport for hosting, and creates an online lobby.
        /// The resultant lobby data, including online lobby information, is returned upon completion.
        /// <returns>
        /// A task representing the asynchronous operation of creating the online lobby.
        /// The task result contains a LobbyCreationData object that holds information about the online lobby created.
        /// </returns>
        private async Task<LobbyCreationData> CreateOnlineLobby()
        {
            NetworkManager.Singleton.Shutdown();

            var lobby = await OnlineLobby.StartHostRelayAsync();
            return new LobbyCreationData { OnlineLobby = lobby };
        }

        /// <summary>
        /// Sets the name of the current lobby.
        /// </summary>
        /// <param name="lobbyName">The name to set for the lobby.</param>
        public void SetLobbyName(string lobbyName)
        {
            if (UseOnline)
                OnlineLobby.SetLobbyName(lobbyName);
        }

        /// <summary>
        /// Sets the password for the current lobby.
        /// </summary>
        /// <param name="password">The password to set for the lobby. If empty or null, the lobby will not require a password.</param>
        public void SetLobbyPassword(string password)
        {
            if (UseOnline)
                OnlineLobby.SetLobbyPassword(password);
        }

        /// <summary>
        /// Sets the maximum number of players allowed in the lobby.
        /// </summary>
        /// <param name="maxPlayers">
        /// The maximum number of players that the lobby can accommodate.
        /// </param>
        public void SetLobbyMaxPlayers(int maxPlayers)
        {
            if (UseOnline)
                OnlineLobby.SetLobbyMaxPlayers(maxPlayers);
        }

        public void CloseLobby()
        {
            if (UseOnline) _ = OnlineLobby.LeaveLobbyAsync(OnlineLobby.CurrentLobby.Id);

            NetworkManager.Singleton.Shutdown();
            if (Settings.StartInSinglePlayer.Value)
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.0", 7777);
                NetworkManager.Singleton.StartHost();
            }
        }
    }
}