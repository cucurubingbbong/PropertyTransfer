using UnityEngine;

public interface IInteractable
{
    public InteractableType InteractableType { get; }

    public abstract void Interact();
}
