using UnityEngine;

public class DragHelper : MonoBehaviour
{
    public ObjectHandler handler;

    // Voor specifieke hondenrassen
    public void StartDogDrag(int breedIndex)
    {
        if (handler != null)
            handler.SelectDogBreed(breedIndex);
    }

    // Voor specifieke speeltjes
    public void StartToyDrag(int toyIndex)
    {
        if (handler != null)
            handler.SelectToyType(toyIndex);
    }
}