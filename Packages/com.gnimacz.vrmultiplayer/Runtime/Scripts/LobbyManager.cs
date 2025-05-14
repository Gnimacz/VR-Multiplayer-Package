using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class LobbyManager : MonoBehaviour
{
    public string RoomCode = "";
    public int maxConnections = 4;
    public UnityTransport transport;
    public Lobby CurrentLobby;
    public Lobby TargetLobby;

    // Lobby Options
    [SerializeField] private bool _isPrivate = false;

    public bool isPrivate
    {
        get => _isPrivate;
        set => _isPrivate = value;
    }

    public string lobbyName = "Lobby Name";
    public string lobbyPassword = "";

    private async void Awake()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }


    public async void StartRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string newJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        SetRoomCode(newJoinCode);

        transport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

        CreateLobbyOptions options = CreateLobbyOptionsWithPassword(newJoinCode);
        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxConnections, options);

        Debug.Log("Joined lobbies: " + string.Join(", ", await LobbyService.Instance.GetJoinedLobbiesAsync()));

        StartCoroutine(SendHeartbeat(CurrentLobby.Id, 5));
        StartCoroutine(KeepLobbyAlive(CurrentLobby.Id, 5));

        NetworkManager.Singleton.StartHost();
    }

    private CreateLobbyOptions CreateLobbyOptionsWithPassword(string RoomCode)
    {
        var options = new CreateLobbyOptions { Data = new() };

        if (isPrivate && !string.IsNullOrEmpty(lobbyPassword))
        {
            string hashedPassword = HashString(lobbyPassword);
            options.Data.Add("HashedPassword", new DataObject(DataObject.VisibilityOptions.Member, hashedPassword));
        }

        options.Data.Add("HasPassword",
            new DataObject(DataObject.VisibilityOptions.Public,
                string.IsNullOrEmpty(lobbyPassword) ? "false" : "true"));
        options.Data.Add("RoomCode", new DataObject(DataObject.VisibilityOptions.Public, RoomCode));

        return options;
    }

    private string HashString(string s)
    {
        using (var algorithm = System.Security.Cryptography.SHA256.Create())
        {
            byte[] data = algorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
            return System.BitConverter.ToString(data).Replace("-", string.Empty); // Cleaner format for hash
        }
    }

    public async void JoinRelay()
    {
        try
        {
            Debug.Log($"CurrentLobby is null? {TargetLobby == null}");
            Debug.Log($"CurrentLobby.Data contains RoomCode? {TargetLobby?.Data?.ContainsKey("RoomCode")}");
            Debug.Log($"RoomCode Value: {TargetLobby?.Data?["RoomCode"]?.Value}");
            if (await IsPasswordValid(TargetLobby))
            {
                // Ensure we're getting the latest data
                CurrentLobby = await LobbyService.Instance.GetLobbyAsync(TargetLobby.Id);
                CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(CurrentLobby.Id);
                await Task.Delay(500);
                CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);

                if (!CurrentLobby.Data.ContainsKey("RoomCode"))
                {
                    Debug.LogError("RoomCode not found in lobby data!");
                    return;
                }

                CurrentLobby.Data.TryGetValue("RoomCode", out var joinCode);


                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Value);
                SetRelayTransportData(allocation);

                NetworkManager.Singleton.StartClient();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during JoinRelay: {ex.Message}");
        }
    }

    public async void QuickJoinRelay()
    {
        try
        {
            CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            if (await IsPasswordValid(CurrentLobby))
            {
                SetRoomCode(CurrentLobby.Data["RoomCode"].Value);

                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(RoomCode);
                SetRelayTransportData(allocation);

                NetworkManager.Singleton.StartClient();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during QuickJoinRelay: {ex.Message}");
        }
    }

    private async Task<bool> IsPasswordValid(Lobby lobby)
    {
        if (lobby.Data["HasPassword"].Value == "true" && lobbyPassword != "")
        {
            string hashedPassword = HashString(lobbyPassword);
            if (lobby.Data["HashedPassword"].Value != hashedPassword)
            {
                Debug.LogError("Incorrect password.");
                await LeaveLobby(lobby.Id);
                return false;
            }
        }

        return true;
    }

    private void SetRelayTransportData(JoinAllocation allocation)
    {
        transport.SetClientRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);
    }

    public void SetRoomCode(string code)
    {
        RoomCode = code;
    }


    public void SetLobbyName(string name) => lobbyName = name;

    public void SetLobbyPassword(string password) => lobbyPassword =
        (password.Length < 8 && password.Length > 0) ? password + "passwordLength" : password;

    public void ShowKeyboard(bool show)
    {
        if (show)
            TouchScreenKeyboard.Open(RoomCode, TouchScreenKeyboardType.Default, false, false, false, false,
                "Enter Room Code");
    }

    private IEnumerator SendHeartbeat(string lobbyId, int seconds)
    {
        var delay = new WaitForSecondsRealtime(seconds);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            Debug.Log("Sent heartbeat ping");
            yield return delay;
        }
    }

    private IEnumerator KeepLobbyAlive(string lobbyId, int seconds)
    {
        var delay = new WaitForSecondsRealtime(seconds);
        while (true)
        {
            try
            {
                LobbyService.Instance.UpdateLobbyAsync(lobbyId, new UpdateLobbyOptions());
                Debug.Log("Lobby kept alive!");
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogError($"Failed to keep lobby alive: {ex.Message}");
                yield break;
            }

            yield return delay;
        }
    }

    private async void OnApplicationQuit()
    {
        var lobbyIds = await LobbyService.Instance.GetJoinedLobbiesAsync();
        foreach (var joinedLobbyId in lobbyIds)
        {
            await LeaveLobby(joinedLobbyId);
        }
    }

    private async Task LeaveLobby(string lobbyId)
    {
        await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);
    }
}