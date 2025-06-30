using UnityEngine;

public class DragHelper : MonoBehaviour
{
    public ObjectHandler handler;

    // Voor honden
    public void StartDogDrag()
    {
        if (handler != null) handler.BeginDragDog();
    }

    // Voor speeltjes
    public void StartToyDrag()
    {
        if (handler != null) handler.BeginDragToy();
    }
}