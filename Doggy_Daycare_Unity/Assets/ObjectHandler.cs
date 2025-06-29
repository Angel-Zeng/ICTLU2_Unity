using TMPro;
using UnityEngine;

public class ObjectHandler : MonoBehaviour
{
    public GameObject dogPrefab;
    public GameObject toyPrefab;

    public TextMeshProUGUI feedbackText;

    private GameObject prefabToPlace;  
    private GameObject ghost;   

    public void BeginDragDog() => BeginDrag(dogPrefab);
    public void BeginDragToy() => BeginDrag(toyPrefab);

    private void BeginDrag(GameObject prefab)
    {
        CancelDrag();

        prefabToPlace = prefab;


        ghost = Instantiate(prefabToPlace);
        ghost.name = "Ghost";
        SetGhostAlpha(0.5f); 
    }

    private void Update()
    {

        if (ghost == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;     
        ghost.transform.position = mousePos;

        if (Input.GetMouseButtonUp(0))
        {
            bool inside =
                mousePos.x >= 0 && mousePos.y >= 0 &&
                mousePos.x <= GameState.SelectedWorldWidth &&
                mousePos.y <= GameState.SelectedWorldHeight;

            if (inside)
            {
                Instantiate(prefabToPlace, mousePos, Quaternion.identity);

                StartCoroutine(APIManager.AddObject(
                    GameState.SelectedWorldId,
                    prefabToPlace.name,    
                    mousePos.x, mousePos.y,
                    res => feedbackText.text =
                        res.Success ? "Placed!" : "Error: " + res.Message));
            }
            else
            {
                feedbackText.text = "Stay within bounds!";
            }

            CancelDrag(); 
        }

        if (Input.GetMouseButtonDown(1))
            CancelDrag();
    }

    private void CancelDrag()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        prefabToPlace = null;
    }

    private void SetGhostAlpha(float alpha)
    {
        SpriteRenderer sr = ghost.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
