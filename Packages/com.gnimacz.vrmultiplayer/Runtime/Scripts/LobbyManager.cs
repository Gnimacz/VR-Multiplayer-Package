using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [Header("Lobby Settings")]
    [SerializeField] private string lobbyName = "Lobby Name";
    [SerializeField] private string lobbyPassword = string.Empty;
    [SerializeField] private bool isPrivate = false;
    [SerializeField] private int maxPlayers = 4;

    [Header("References")]
    [SerializeField] private UnityTransport transport;

    public string RoomCode { get; private set; } = string.Empty;
    public Lobby CurrentLobby { get; private set; }

    private const float HeartbeatInterval = 5f;
    private Coroutine heartbeatCoroutine;
    private Coroutine keepAliveCoroutine;

    private async void Awake()
    {
        await InitializeServicesAsync();
    }

    private async Task InitializeServicesAsync()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    #region Host Lobby

    public async Task StartHostRelayAsync()
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            RoomCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            ConfigureTransportHost(allocation);

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName,
                maxPlayers,
                BuildCreateOptions(RoomCode)
            );

            Debug.Log($"Lobby created (ID: {CurrentLobby.Id}), RoomCode: {RoomCode}");

            heartbeatCoroutine = StartCoroutine(PeriodicHeartbeat(CurrentLobby.Id, HeartbeatInterval));
            keepAliveCoroutine = StartCoroutine(PeriodicKeepAlive(CurrentLobby.Id, HeartbeatInterval));

            NetworkManager.Singleton.StartHost();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start host relay: {ex.Message}");
        }
    }

    private CreateLobbyOptions BuildCreateOptions(string roomCode)
    {
        var data = new Dictionary<string, DataObject>
        {
            { "RoomCode", new DataObject(DataObject.VisibilityOptions.Public, roomCode) },
            { "HasPassword", new DataObject(DataObject.VisibilityOptions.Public, (!string.IsNullOrEmpty(lobbyPassword)).ToString().ToLower()) }
        };

        if (isPrivate && !string.IsNullOrEmpty(lobbyPassword))
        {
            string hashed = HashString(lobbyPassword);
            data.Add("HashedPassword", new DataObject(DataObject.VisibilityOptions.Member, hashed));
        }

        return new CreateLobbyOptions { Data = data };
    }

    private void ConfigureTransportHost(Allocation allocation)
    {
        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );
    }

    #endregion

    #region Join Lobby

    public async Task JoinRelayAsync(string lobbyId)
    {
        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
            await AttemptJoinAsync(lobby);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error joining relay: {ex.Message}");
        }
    }

    public async Task QuickJoinRelayAsync()
    {
        try
        {
            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            await AttemptJoinAsync(lobby);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in quick join relay: {ex.Message}");
        }
    }

    private async Task AttemptJoinAsync(Lobby lobby)
    {
        if (!await ValidatePasswordAsync(lobby)) return;

        CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
        CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);

        if (!CurrentLobby.Data.TryGetValue("RoomCode", out var codeObj))
        {
            Debug.LogError("No RoomCode in lobby data.");
            return;
        }

        RoomCode = codeObj.Value;
        var allocation = await RelayService.Instance.JoinAllocationAsync(RoomCode);
        ConfigureTransportClient(allocation);

        NetworkManager.Singleton.StartClient();
    }

    private async Task<bool> ValidatePasswordAsync(Lobby lobby)
    {
        if (lobby.Data.TryGetValue("HasPassword", out var hasPwd) && hasPwd.Value == "true")
        {
            if (string.IsNullOrEmpty(lobbyPassword))
            {
                Debug.LogError("Password required but none provided.");
                await LeaveLobbyAsync(lobby.Id);
                return false;
            }

            string hashed = HashString(lobbyPassword);
            if (!lobby.Data.TryGetValue("HashedPassword", out var stored) || stored.Value != hashed)
            {
                Debug.LogError("Invalid lobby password.");
                await LeaveLobbyAsync(lobby.Id);
                return false;
            }
        }
        return true;
    }

    private void ConfigureTransportClient(JoinAllocation allocation)
    {
        transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData
        );
    }

    #endregion

    #region Lobby Maintenance

    private IEnumerator PeriodicHeartbeat(string lobbyId, float interval)
    {
        var wait = new WaitForSecondsRealtime(interval);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return wait;
        }
    }

    private IEnumerator PeriodicKeepAlive(string lobbyId, float interval)
    {
        var wait = new WaitForSecondsRealtime(interval);
        while (true)
        {
            try
            {
                LobbyService.Instance.UpdateLobbyAsync(lobbyId, new UpdateLobbyOptions());
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogError($"Keep-alive failed: {ex.Message}");
                yield break;
            }
            yield return wait;
        }
    }

    #endregion

    private async void OnApplicationQuit()
    {
        if (CurrentLobby != null)
        {
            await LeaveLobbyAsync(CurrentLobby.Id);
        }
    }

    private async Task LeaveLobbyAsync(string lobbyId)
    {
        await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);
    }

    #region Utilities

    private static string HashString(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    #endregion

    #region Editor Helpers

    // Exposed to UI
    public void SetLobbyName(string name) => lobbyName = name;
    public void SetLobbyPassword(string password) => lobbyPassword = password;

    public void ShowKeyboard()
    {
        TouchScreenKeyboard.Open(RoomCode, TouchScreenKeyboardType.Default, false, false, false, false, "Enter Room Code");
    }

    #endregion
}
