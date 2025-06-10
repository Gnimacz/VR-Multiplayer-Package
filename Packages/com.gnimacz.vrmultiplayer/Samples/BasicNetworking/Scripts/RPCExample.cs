using Unity.Netcode;
using UnityEngine;

namespace gnimacz.vrmultiplayer.Samples.BasicNetworking.Scripts
{
    public class RPCExample : NetworkBehaviour
    {
        /// <summary>
        /// An example of a Remote Procedure Call (RPC) that can be invoked by any client.
        /// This method disables the GameObject this script is attached to,
        /// this is executed on all clients that are connected to the network.
        /// </summary>
        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void ChopTreeRpc()
        {
            gameObject.SetActive(false);
        }
    }
}
