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
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        StartCoroutine(LoadWorldThenEnableDrag()); //laden van wereld en activeren van die drag functie
    }

    private IEnumerator LoadWorldThenEnableDrag()
    {
        //Wereld moet geladen zijn en mag ook niet 0 zijn want de index begint daar niet
        if (GameState.SelectedWorldId == 0)
        {
            feedbackText.text = "Geen wereld gevonden met 0";
            yield break;
        }

        int worldId = GameState.SelectedWorldId;

        //Laden van wereldgegevens
        yield return APIManager.GetWorldObjects(worldId, data =>
        {
            if (data == null)
            {
                feedbackText.text = "Fout bij het laden van de wereld.";
                return;
            }

            // De text updaten met de gekozen wereldnaam
            worldNameText.text = data.world.name;

            //de grenzen van de wereld aangeven
            DrawBorder(data.world.width, data.world.height);

            // Instantieer honden en speeltjes
            foreach (var obj in data.objects)
            {
                GameObject prefab = obj.type == "Dog" ? dogPrefab : toyPrefab;
                Instantiate(prefab, new Vector3(obj.x, obj.y, 0), Quaternion.identity);
            }

            feedbackText.text = "Sleep een hond of speeltje!";
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

    public void BeginDragDog()
    {
        StartDrag(dogPrefab);
    }
    public void BeginDragToy() => StartDrag(toyPrefab);

    //De sleepfunc
    private void StartDrag(GameObject prefab)
    {
        if (isDragging) return;

        prefabToPlace = prefab;
        isDragging = true;

        dragPreview = Instantiate(prefab);
        dragPreview.name = "DragPreview";

        SetAlpha(dragPreview, 0.5f); // even semi maken 

        // Colliders uitschakelen tijdens het slepen
        var colliders = dragPreview.GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Voeg tijdelijk een BoxCollider2D toe voor betere hit detection
        if (dragPreview.GetComponent<Collider2D>() == null)
        {
            dragPreview.AddComponent<BoxCollider2D>();
        }
    }

    // Update loop voor slepen
    private void Update()
    {
        if (!isDragging) return;

        //positie van de muisklik krijgen
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // Belangrijk: afstand tot camera instellen
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0; // Zet Z op 0 voor 2D

        // Update preview positie
        if (dragPreview != null)
        {
            dragPreview.transform.position = worldPos;
        }

        // bij het loslaten van de muis, plaats object
        if (Input.GetMouseButtonUp(0))
        {
            TryPlaceObject(worldPos);
        }

        // Annuleren met rechtermuis
        if (Input.GetMouseButtonDown(1))
        {
            CancelDrag();
            feedbackText.text = "Plaatsing geannuleerd";
        }
    }

    // Probeert object te plaatsen
    private void TryPlaceObject(Vector3 position)
    {
        // kijken of er een prefab is om te plaatsten 
        if (IsOverTrashCan())
        {
            feedbackText.text = "Plaatsing geannuleerd!";
            CancelDrag();
            return;
        }

        //checken of de positie wel binnen de grenzen van de wereld valt
        bool inBounds = position.x >= 0 && position.y >= 0 &&
                       position.x <= GameState.SelectedWorldWidth &&
                       position.y <= GameState.SelectedWorldHeight;

        if (!inBounds)
        {
            feedbackText.text = "Blijf binnen de grenzen!";
            CancelDrag();
            return;
        }

        // PLAATSEN!!
        Instantiate(prefabToPlace, position, Quaternion.identity);

        //De informatie naar de server sturen
        StartCoroutine(APIManager.AddObject(
            GameState.SelectedWorldId,
            prefabToPlace == dogPrefab ? "Dog" : "Toy",
            position.x,
            position.y,
            result =>
            {
                feedbackText.text = result.Success
                    ? "Succesvol geplaatst!"
                    : "Oops: " + result.Message;
            }));

        CancelDrag();
    }

    private bool IsOverTrashCan()
    {
        // Controleer of de muis over de prullenbak is
        if (RectTransformUtility.RectangleContainsScreenPoint(
            trashZone,
            Input.mousePosition,
            mainCamera))
        {
            // Geef visuele feedback
            if (dragPreview != null)
            {
                var renderer = dragPreview.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = new Color(1, 0.5f, 0.5f, 0.5f);
                }
            }
            return true;
        }
        return false;
    }

    private void CancelDrag()
    {
        isDragging = false;
        prefabToPlace = null;

        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }

    private void SetAlpha(GameObject obj, float alpha)
    {
        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}