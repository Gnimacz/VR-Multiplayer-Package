using gnimacz.vrmultiplayer.Lobbies;
using UnityEngine;

namespace gnimacz.vrmultiplayer.Samples.BasicNetworking.Scripts
{
    /// <summary>
    /// Represents a menu interface for lobby connection management.
    /// </summary>
    /// <remarks>
    /// This class provides functionality for setting a lobby room code
    /// and initiating a connection to the lobby using the provided code.
    /// It interacts with the <c>LobbyManager</c> to manage the connection process.
    /// This class is only set up for a local lobby connection. Support for online lobbies will have to be done by the user.
    /// </remarks>
    public class LobbyConnectionMenu : MonoBehaviour
    {
        /// <summary>
        /// Represents the unique code of a lobby, used to join a specific game session.
        /// </summary>
        /// <remarks>
        /// This variable is utilized in the process of connecting to a lobby within the game.
        /// It serves as an identifier to allow players to enter the desired lobby.
        /// </remarks>
        private string _lobbyCode;

        /// <summary>
        /// Sets the code for the room to be used for the lobby connection.
        /// </summary>
        /// <param name="code">The room code to be used for joining a lobby.</param>
        public void SetRoomCode(string code)
        {
            _lobbyCode = code;
        }

        /// <summary>
        /// Attempts to connect to a lobby using the currently set lobby code.
        /// </summary>
        /// <remarks>
        /// This method checks if a lobby code has been set before attempting to join a lobby.
        /// If the lobby code is empty or null, an error message is logged, and the method exits without performing any action.
        /// If a valid lobby code is present, it invokes the system to join the specified lobby.
        /// </remarks>
        /// <exception cref="UnityEngine.Debug.LogError">
        /// Thrown if the lobby code is empty or null.
        /// </exception>
        public void ConnectToLobby()
        {
            if (string.IsNullOrEmpty(_lobbyCode))
            {
                Debug.LogError("Lobby code cannot be empty.");
                return;
            }

            LobbyManager.Instance.JoinLobby(_lobbyCode);
        }
    }
}