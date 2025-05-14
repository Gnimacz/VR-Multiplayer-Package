using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CustomLobby
{
    public class Lobby
    {
        private static readonly string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static string CreateLocalLobby()
        {
            string ip = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList
                .First(x => (x.ToString().Split('.')[^1] != "1") &&
                            (x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)).ToString();

            int port = UnityEngine.Random.Range(49152, 65535);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, (ushort)port);
            Debug.LogWarning($"Creating lobby at {ip}:{port}");

            return GenerateLobbyCode(ip, port);
        }

        public static string GenerateLobbyCode(string ip, int port)
        {
            return EncodeRoomCode(ip, port);
        }

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

        public static (string, int) DecodeRoomCode(string roomCode)
        {
            BigInteger number = DecodeBase58(roomCode);
            var bytes = number.ToByteArray().ToArray();

            if (bytes.Length < 4) throw new Exception("Invalid room code");

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

        public static string Base58Encode(byte[] bytes)
        {
            BigInteger number = new BigInteger(bytes.Concat(new byte[] { 0 }).ToArray());

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

        private static BigInteger DecodeBase58(string input)
        {
            BigInteger number = 0;
            foreach (char c in input)
            {
                number = number * Alphabet.Length + Alphabet.IndexOf(c);
            }

            return number;
        }
    }
}