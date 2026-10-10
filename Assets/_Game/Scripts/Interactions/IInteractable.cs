using UnityEngine;

namespace Game.Interactions
{
    /// <summary>
    /// Contract for all physical objects, switches, tools, and downed friends that can be interacted with or carried.
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanBeGrabbed { get; }
        
        void OnInteract(GameObject interactor);
        void OnGrab(Transform holdPoint, Rigidbody interactorBody);
        void OnRelease(Vector3 throwVelocity);
    }
}
