using System;
using System.Linq;
using System.Runtime.CompilerServices;
using CustomLobby;
using UnityEngine;

public class LobbyScript : MonoBehaviour
{
    public string testIP = "192.168.15.16";

    //Singleton
    public static LobbyScript Instance { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        string roomCode = Lobby.CreateLocalLobby();
        Debug.Log(roomCode);
        Debug.Log(Lobby.DecodeRoomCode(roomCode));
    }

    public static string GenerateRoomCode()
    {
        // Get the IP address of the local machine
        string ip = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList
            .First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();

        // Create a room code based on the IP address
        string[] ipParts = ip.Split('.');
        string roomCode = "";
        for (int i = 0; i < ipParts.Length; i++)
        {
            var individualNumbers = ipParts[i].ToCharArray();
            foreach (char digitString in individualNumbers)
            {
                int digit = int.Parse(digitString.ToString());
                char letter = (char)('A' + digit);
                roomCode += letter;
            }
        }

        // get a random port
        int port = UnityEngine.Random.Range(49152, 65566);
        Debug.Log($"IP and Generated Port: {ip}:{port}");

        //convert it to a string
        string portString = port.ToString();
        var individualPortNumbers = portString.ToCharArray();
        foreach (char digitString in individualPortNumbers)
        {
            int digit = int.Parse(digitString.ToString());
            char letter = (char)('A' + digit);
            roomCode += letter;
        }

        return roomCode;
    }

    public void DecodeRoomCode(string roomCode, out string ip, out string address)
    {
        // Split the room code into the IP and port parts
        char[] portArray = roomCode.ToCharArray()[^5..];
        char[] ipParts = roomCode.ToCharArray()[..^5];
        
        // Decode the port part
        string portString = "";
        foreach (var ipPart in portArray)
        {
            int digit = ipPart - 'A';
            portString += digit.ToString();
        }
        
        // Decode the IP part
        string ipString = "";
        foreach (var ipPart in ipParts)
        {
            Debug.Log($"decoding {ipPart}");
            int digit = ipPart - 'A';
            ipString += digit.ToString();
        }
        Debug.Log($"Incoming code: {roomCode} \n\n" +
                  $"Decoded IP and Port: {ipString}:{portString}");
        
        ip = "null";
        address = "null";
    }
}