using System;
using gnimacz.vrmultiplayer.Lobbies;
using TMPro;
using UnityEngine;
using VRMultiplayer.Editor;

namespace BasicNetworking.Scripts
{
    /// <summary>
    /// Provides functionality to manage and interact with the lobby interface, including setting lobby parameters
    /// such as name, maximum players, and password. It also handles the display of the room code when a lobby is created.
    /// </summary>
    public class LobbyMenuBehavior : MonoBehaviour
    {
        /// <summary>
        /// A text component used to display the current room code for a locally-hosted lobby.
        /// This field is updated when the lobby is created and the room code is generated.
        /// </summary>
        public TextMeshProUGUI roomCodeText;

        /// <summary>
        /// Sets the name of the lobby by delegating to the LobbyManager instance.
        /// </summary>
        /// <param name="name">The name to set for the lobby.</param>
        public void SetLobbyName(string name)
        {
            LobbyManager.Instance.SetLobbyName(name);
        }

        /// <summary>
        /// Sets the maximum number of players allowed in the lobby.
        /// </summary>
        /// <param name="maxPlayers">
        /// The maximum number of players that the lobby can accommodate.
        /// </param>
        public void SetLobbyMaxPlayers(int maxPlayers)
        {
            LobbyManager.Instance.SetLobbyMaxPlayers(maxPlayers);
        }

        /// <summary>
        /// Sets the password for the current lobby.
        /// </summary>
        /// <param name="password">The password to be set for the lobby. If left empty, the lobby will be password-free.</param>
        public void SetLobbyPassword(string password)
        {
            LobbyManager.Instance.SetLobbyPassword(password);
        }

        /// <summary>
        /// Starts the lobby initialization process. If the setting for using an online lobby is disabled,
        /// it triggers the display of a room code for a local session.
        /// </summary>
        public void StartLobby()
        {
            if(!Settings.UseOnlineLobby.Value) DisplayRoomCode();
        }

        /// <summary>
        /// Displays the room code for a created local lobby.
        /// This method asynchronously creates a lobby, retrieves the local room code,
        /// and sets it to the designated UI text element.
        /// </summary>
        /// <remarks>
        /// This method ensures that the local room code corresponding to the created lobby is displayed
        /// on the UI element specified by <c>roomCodeText</c>.
        /// It is invoked only when the online lobby mode is disabled.
        /// The creation of a lobby and retrieval of the room code is an asynchronous operation.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the <c>LobbyManager</c> instance is not initialized before invoking this method.
        /// </exception>
        private async void DisplayRoomCode()
        {
            var room = await LobbyManager.Instance.CreateLobby();
            roomCodeText.text = room.LocalRoomCode;
        }
    }
}