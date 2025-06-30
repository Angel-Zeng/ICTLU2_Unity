using UnityEngine;
using UnityEngine.EventSystems;

public class DragHelper : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("Prefab that will be dragged when this icon is pressed")]
    public GameObject prefab;
    public ObjectHandler handler;


    public void OnPointerDown(PointerEventData eventData)
    {
        if (handler != null && prefab != null)
            handler.StartDrag(prefab);
    }
}