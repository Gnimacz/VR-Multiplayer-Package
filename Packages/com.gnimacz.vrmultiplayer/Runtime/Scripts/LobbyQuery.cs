using System;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEditor;
using UnityEngine.UI;

public class LobbyQuery : MonoBehaviour
{
    public QueryLobbiesOptions options = new();
    public async void QueryLobbies()
    {
        try
        {
            var lobbies = await LobbyService.Instance.QueryLobbiesAsync();
            
            if (lobbies.Results.Count == 0) Debug.Log("No lobbies found");
            foreach (var lobby in lobbies.Results)
            {
                Debug.Log(lobby.Name);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}