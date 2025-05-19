using Unity.Netcode.Components;

namespace gnimacz.vrmultiplayer.Network
{
    public class ClientNetworkedGameObject : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
