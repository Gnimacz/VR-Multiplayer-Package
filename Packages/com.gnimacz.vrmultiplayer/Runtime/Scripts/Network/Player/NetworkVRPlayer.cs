using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a virtual reality player instance in a multiplayer environment.
/// Inherits from NetworkBehaviour to support networked functionality.
/// </summary>
public class VRPlayer : NetworkBehaviour
{
    /// <summary>
    /// Represents the root Transform of the VR player in the networked player hierarchy.
    /// This Transform serves as the base reference point for the player's overall positioning,
    /// rotation, and scaling in the virtual environment.
    /// </summary>
    public Transform Root;

    /// <summary>
    /// Represents the head transform of the VR player.
    /// This is used to synchronize the position, rotation, and scale
    /// of the player's head with its corresponding reference in the VRPlayerReference instance.
    /// </summary>
    public Transform Head;

    /// <summary>
    /// Represents the transform of the player's left hand in a virtual reality environment.
    /// This is updated based on the corresponding left hand transform from the
    /// VRPlayerReference instance to ensure synchronization.
    /// </summary>
    public Transform LeftHand;

    /// <summary>
    /// Represents the transform of the right hand of the VR player.
    /// This property is used to synchronize the position, rotation,
    /// and scale of the player's right hand across the network.
    /// </summary>
    public Transform RightHand;

    /// <summary>
    /// Called when the network object is spawned. This method is invoked once the object has been initialized
    /// by the network and is ready for networked behavior. It can be overridden to handle setup specific
    /// to the networked environment.
    /// Specifically in this implementation, if the calling object is owned by the current client, it disables
    /// all Renderer components within the object's child hierarchy.
    /// </summary>
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

    /// <summary>
    /// Synchronizes the current player's transforms (Root, Head, LeftHand, RightHand) with the corresponding
    /// transforms in the VRPlayerReference instance, ensuring that the local VR player representation matches
    /// the reference positions, rotations, and scales.
    /// </summary>
    /// <remarks>
    /// This method is automatically called during each frame update and is responsible for maintaining the
    /// synchronization between the player's transforms and the reference transforms on the local machine.
    /// The update process occurs only for the owner of the networked VR player instance.
    /// </remarks>
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
