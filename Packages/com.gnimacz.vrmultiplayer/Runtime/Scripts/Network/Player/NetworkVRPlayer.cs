using Unity.Netcode;
using UnityEngine;

public class VRPlayer : NetworkBehaviour
{
    public Transform Root;
    public Transform Head;
    public Transform LeftHand;
    public Transform RightHand;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Remove the renderer of all the owners children
        if (IsOwner)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
        }
    }

    private void Update()
    {
        // Update the VRPlayerReference with the current player's transform
        if (IsOwner)
        {
            Root.transform.position = VRPlayerReference.Instance.Root.transform.position ;
            Root.transform.rotation = VRPlayerReference.Instance.Root.transform.rotation ;
            Root.transform.localScale = VRPlayerReference.Instance.Root.transform.localScale ;
            
            Head.transform.position = VRPlayerReference.Instance.Head.transform.position ;
            Head.transform.rotation = VRPlayerReference.Instance.Head.transform.rotation ;
            Head.transform.localScale = VRPlayerReference.Instance.Head.transform.localScale ;
            
            LeftHand.transform.position = VRPlayerReference.Instance.LeftHand.transform.position ;
            LeftHand.transform.rotation = VRPlayerReference.Instance.LeftHand.transform.rotation;
            LeftHand.transform.localScale = VRPlayerReference.Instance.LeftHand.transform.localScale ;
            
            RightHand.transform.position = VRPlayerReference.Instance.RightHand.transform.position ;
            RightHand.transform.rotation = VRPlayerReference.Instance.RightHand.transform.rotation ;
            RightHand.transform.localScale = VRPlayerReference.Instance.RightHand.transform.localScale ;
        }
    }
}
