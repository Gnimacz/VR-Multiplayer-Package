using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkedGameObject : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
