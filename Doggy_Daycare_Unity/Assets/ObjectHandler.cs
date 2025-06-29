using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//Dit script heeft mijn heeft mijn mentale gezondheid naar een dieptepunt gebracht, de honden willen niet. 
public class ObjectHandler : MonoBehaviour
{
    public GameObject dogPrefab; 
    public GameObject toyPrefab;

    public TextMeshProUGUI worldNameText;
    public TextMeshProUGUI feedbackText;
    public LineRenderer borderLine;
    public RectTransform trashZone;

    private GameObject prefabToPlace;
    private bool isDragging = false;
    private GameObject dragPreview;

    private void Start()
    {
        StartCoroutine(LoadWorldThenEnableDrag()); //laden van wereld en activeren van die drag functie
    }


    private IEnumerator LoadWorldThenEnableDrag()
    {
        //Wereld moet geladen zijn en mag ook niet 0 zijn want de index begint daar niet
        if (GameState.SelectedWorldId == 0)
        {
            feedbackText.text = "No world on 0";
            yield break;
        }

        int wid = GameState.SelectedWorldId;

        //Laden van wereldgegevens
        yield return APIManager.GetWorldObjects(wid, data =>
        {
            if (data == null) return;

            // De text updaten met de gekozen wereldnaam
            worldNameText.text = $"{data.world.name}";

            //de grenzen van de wereld aangeven
            DrawBorder(data.world.width, data.world.height);

            // Instantieer honden en speeltjes
            foreach (var obj in data.objects)
            {
                GameObject prefab = obj.type == "Dog" ? dogPrefab : toyPrefab;
                Instantiate(prefab, new Vector3(obj.x, obj.y, 0), Quaternion.identity);
            }
        });
    }

    //Dit tekent de randen van de wereld, snapte ik ook niet zo goed dus gestolen van iemand anders :))
    private void DrawBorder(int width, int height)
    {
        borderLine.positionCount = 5;
        borderLine.SetPositions(new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(width, 0, 0),
            new Vector3(width, height, 0),
            new Vector3(0, height, 0),
            new Vector3(0, 0, 0)
        });
    }

    //Dit werkt niet D: het zou moeten werken maar de honden willen niet :(
    public void BeginDragDog() => StartDrag(dogPrefab);
    //ja hetzelfde weer
    public void BeginDragToy() => StartDrag(toyPrefab);

    //De sleepfunc
    private void StartDrag(GameObject prefab)
    {
        if (isDragging) return;

        prefabToPlace = prefab;
        isDragging = true;
    }

    // Update loop voor slepen
    private void Update()
    {
        if (!isDragging) return;

        //positie van de muisklik krijgen
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        // bij het loslaten van de muis, plaat object
        if (Input.GetMouseButtonUp(0))
        {
            TryPlaceObject(worldPos);
        }
    }

    // Probeert object te plaatsen
    private void TryPlaceObject(Vector3 position)
    {
        // kijken of er een prefab is om te plaatsten 
        if (RectTransformUtility.RectangleContainsScreenPoint(trashZone, Input.mousePosition))
        {
            feedbackText.text = "Placement cancelled";
            CancelDrag();
            return;
        }

        //checken of de positie wel binnen de grenzen van de wereld valt
        bool inBounds = position.x >= 0 && position.y >= 0 &&
                       position.x <= GameState.SelectedWorldWidth &&
                       position.y <= GameState.SelectedWorldHeight;

        if (!inBounds)
        {
            feedbackText.text = "Stay within bounds!";
            CancelDrag();
            return;
        }

        // PLAATSEN!!
        Instantiate(prefabToPlace, position, Quaternion.identity);

        //De informatie naar de server sturen
        StartCoroutine(APIManager.AddObject(
            GameState.SelectedWorldId,
            prefabToPlace == dogPrefab ? "Dog" : "Toy",
            position.x, position.y,
            result => feedbackText.text = result.Success ? "Placed!" : "Error: " + result.Message));

        CancelDrag();
    }

    //Drag annuleren
    private void CancelDrag()
    {
        isDragging = false;
        prefabToPlace = null;

        if (dragPreview) Destroy(dragPreview);
        dragPreview = null;
    }

}