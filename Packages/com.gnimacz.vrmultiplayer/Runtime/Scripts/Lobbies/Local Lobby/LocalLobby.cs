using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace gnimacz.vrmultiplayer.Lobbies.Local_Lobby
{
    /// <summary>
    /// Represents a utility class that provides functionality for creating and managing network lobbies.
    /// </summary>
    public class LocalLobby
    {
        /// <summary>
        /// Represents the set of characters used within the custom base58 encoding and decoding logic.
        /// This variable defines a unique sequence of alphanumeric characters excluding visually similar ones (e.g., '0', 'O', 'I', 'l')
        /// to ensure better clarity and usability during encoding/decoding of data.
        /// </summary>
        private static readonly string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// Creates a local lobby by generating connection data, such as IP address and port,
        /// and initializes the network transport layer with this information.
        /// Also, generates a unique lobby code based on the connection data.
        /// <returns>The generated lobby code for the local lobby as a string. The code can be used for
        /// joining or sharing the lobby with others.
        public static string Create()
        {
            string ip = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList
                .First(x => (x.ToString().Split('.')[^1] != "1") &&
                            (x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)).ToString();

            int port = UnityEngine.Random.Range(49152, 65535);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, (ushort)port);
            Debug.LogWarning($"Creating lobby at {ip}:{port}");

            return GenerateLobbyCode(ip, port);
        }

        /// Generates a unique lobby code based on the provided IP address and port number.
        /// <param name="ip">The IP address used to create the lobby.</param>
        /// <param name="port">The port number used to create the lobby.</param>
        /// <return>A string representing the encoded unique lobby code.</return>
        public static string GenerateLobbyCode(string ip, int port)
        {
            return EncodeRoomCode(ip, port);
        }

        /// Encodes the given IP address and port into a room code suitable for distribution in a private lobby setup.
        /// The encoded room code combines the IP address and port in an optimized format and converts it to a Base58 string representation.
        /// This method supports private IP address ranges and ensures the accuracy and portability of the room code.
        /// An exception is thrown for invalid or non-private IP addresses.
        /// <param name="ip">The private IP address of the server running the lobby, represented as a string.</param>
        /// <param name="port">The port number of the server running the lobby, represented as an integer.</param>
        /// <returns>A Base58 encoded string representing the room code, derived from the given IP address and port.</returns>
        /// <exception cref="Exception">Thrown when the provided IP address is not within the private address ranges.</exception>
        private static string EncodeRoomCode(string ip, int port)
        {
            var ipParts = ip.Split('.').Select(byte.Parse).ToArray();
            uint packedValue = 0;

            // Encode the private IP range
            if (ipParts[0] == 192 && ipParts[1] == 168)
            {
                // 192.168.x.x → (00) + last 2 bytes
                packedValue = (uint)(0b00 << 30 | (ipParts[2] << 8) | ipParts[3]);
            }
            else if (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31))
            {
                // 172.16-31.x.x → (01) + (range) + last 2 bytes
                packedValue = (uint)(0b01 << 30 | ((ipParts[1] - 16) << 16) | (ipParts[2] << 8) | ipParts[3]);
            }
            else if (ipParts[0] == 10)
            {
                // 10.x.x.x → (10) + last 3 bytes
                packedValue = (uint)(0b10 << 30 | (ipParts[1] << 16) | (ipParts[2] << 8) | ipParts[3]);
            }
            else
            {
                throw new Exception("IP is not a private address.");
            }

            // Store the port as an offset from 49152
            ushort portDelta = (ushort)(port - 49152);
            packedValue = (packedValue << 14) | portDelta;

            return Base58Encode(BitConverter.GetBytes(packedValue));
        }

        /// Decodes a given room code into its respective IP address and port.
        /// The room code is a compressed representation of network connection details
        /// and can be converted back to retrieve the original data.
        /// <param name="roomCode">The encoded room code string representing the IP and port.</param>
        /// <returns>A tuple containing the decoded IP address as a string and the port as an integer.</returns>
        /// <exception cref="System.Exception">
        /// Thrown if the room code is invalid or if the IP type specified in the room code is unrecognized.
        /// </exception>
        public static (string, int) DecodeRoomCode(string roomCode)
        {
            byte[] bytes = DecodeBase58ToBytes(roomCode);

            if (bytes.Length < 4) throw new Exception($"Invalid room code. Byte array too short: {bytes.Length}");

            uint packedValue = BitConverter.ToUInt32(bytes, 0);

            // Extract the port delta and restore full port
            ushort port = (ushort)((packedValue & 0x3FFF) + 49152);
            packedValue >>= 14; // Remove port bits

            // Extract IP type
            int ipType = (int)(packedValue >> 30);
            packedValue &= 0x3FFFFFFF; // Remove the 2 bits of IP type

            string ip;

            if (ipType == 0b00)
            {
                // 192.168.x.x
                byte x = (byte)(packedValue >> 8);
                byte y = (byte)(packedValue & 0xFF);
                ip = $"192.168.{x}.{y}";
            }
            else if (ipType == 0b01)
            {
                // 172.16-31.x.x
                byte range = (byte)(packedValue >> 16);
                byte x = (byte)((packedValue >> 8) & 0xFF);
                byte y = (byte)(packedValue & 0xFF);
                ip = $"172.{range + 16}.{x}.{y}";
            }
            else if (ipType == 0b10)
            {
                // 10.x.x.x
                byte a = (byte)(packedValue >> 16);
                byte b = (byte)((packedValue >> 8) & 0xFF);
                byte c = (byte)(packedValue & 0xFF);
                ip = $"10.{a}.{b}.{c}";
            }
            else
            {
                throw new Exception("Invalid IP type.");
            }

            return (ip, port);
        }

        /// <summary>
        /// Encodes a byte array into a Base58 string representation.
        /// </summary>
        /// <param name="bytes">The byte array to encode.</param>
        /// <returns>A Base58 encoded string representing the input byte array.</returns>
        public static string Base58Encode(byte[] bytes)
        {
            BigInteger number = new BigInteger(bytes.Concat(new byte[] { 0 }).ToArray(), isUnsigned: true,
                isBigEndian: true);

            StringBuilder result = new StringBuilder();
            while (number > 0)
            {
                int remainder = (int)(number % Alphabet.Length);
                number /= Alphabet.Length;
                result.Insert(0, Alphabet[remainder]);
            }

            foreach (byte b in bytes)
            {
                if (b == 0) result.Insert(0, '1');
                else break;
            }

            return result.ToString();
        }

        /// Decodes a Base58-encoded string into a BigInteger.
        /// <param name="input">The Base58-encoded string to decode.</param>
        /// <returns>A BigInteger representing the decoded value from the Base58 string.</returns>
        private static BigInteger DecodeBase58(string input)
        {
            BigInteger number = 0;
            foreach (char c in input)
            {
                number = number * Alphabet.Length + Alphabet.IndexOf(c);
            }

            return number;
        }

        static byte[] DecodeBase58ToBytes(string input)
        {
            // Decode Base58 into BigInteger using helper code
            BigInteger bi = DecodeBase58(input);
            return bi.ToByteArray(isUnsigned: true, isBigEndian: true);
        }
    }
}