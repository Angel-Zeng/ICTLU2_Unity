using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//Dit script heeft mijn heeft mijn mentale gezondheid naar een dieptepunt gebracht, de honden willen niet. 
public class ObjectHandler : MonoBehaviour
{
    // Enums voor type selectie
    public enum DogBreed { Dachshund, FrenchBulldog, ShibaInu, Poodle }
    public enum ToyType { Ball, Bone, Frisbee }

    public GameObject dachshundPrefab;
    public GameObject frenchBulldogPrefab;
    public GameObject shibaInuPrefab;
    public GameObject poodlePrefab;

    public GameObject ballPrefab;
    public GameObject bonePrefab;
    public GameObject frisbeePrefab;

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
                // Voor nu gebruiken we standaard prefabs - later aanpassen
                GameObject prefab = obj.type.StartsWith("Dog") ? dachshundPrefab : ballPrefab;
                Instantiate(prefab, new Vector3(obj.x, obj.y, 0), Quaternion.identity);
            }

            feedbackText.text = "Sleep een hond of speeltje!";
        });
    }

    //Dit tekent de randen van de wereld, snapte ik ook niet zo goed dus gestolen van iemand anders :))
    private void DrawBorder(float width, float height)
    {
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        borderLine.positionCount = 5;
        borderLine.SetPositions(new Vector3[]
        {
        new Vector3(-halfW, -halfH, 0),   // bottom-left
        new Vector3( halfW, -halfH, 0),   // bottom-right
        new Vector3( halfW,  halfH, 0),   // top-right
        new Vector3(-halfW,  halfH, 0),   // top-left
        new Vector3(-halfW, -halfH, 0)    // back to start
        });
    }

    // Nieuwe methodes voor selectie
    public void SelectDogBreed(int breedIndex)
    {
        StartDrag(GetDogPrefab((DogBreed)breedIndex));
    }

    public void SelectToyType(int toyIndex)
    {
        StartDrag(GetToyPrefab((ToyType)toyIndex));
    }

    private GameObject GetDogPrefab(DogBreed breed)
    {
        switch (breed)
        {
            case DogBreed.Dachshund:
                return dachshundPrefab;
            case DogBreed.FrenchBulldog:
                return frenchBulldogPrefab;
            case DogBreed.ShibaInu:
                return shibaInuPrefab;
            case DogBreed.Poodle:
                return poodlePrefab;
            default:
                Debug.LogError("Onbekend hondenras");
                return dachshundPrefab;
        }
    }

    private GameObject GetToyPrefab(ToyType toy)
    {
        switch (toy)
        {
            case ToyType.Ball:
                return ballPrefab;
            case ToyType.Bone:
                return bonePrefab;
            case ToyType.Frisbee:
                return frisbeePrefab;
            default:
                Debug.LogError("Onbekend speeltje");
                return ballPrefab;
        }
    }

    //De sleepfunctie
    public void StartDrag(GameObject prefab)
    {
        if (isDragging) return;

        prefabToPlace = prefab;
        isDragging = true;

        dragPreview = Instantiate(prefab);
        dragPreview.name = "DragPreview";

        SetAlpha(dragPreview, 0.5f); // even semi maken 

        var colliders = dragPreview.GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // BoxCollider2D toegevoegd 
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

        float halfW = GameState.SelectedWorldWidth * 0.5f;
        float halfH = GameState.SelectedWorldHeight * 0.5f;

        bool inBounds = position.x >= -halfW && position.x <= halfW &&
                        position.y >= -halfH && position.y <= halfH;

        if (!inBounds)
        {
            feedbackText.text = "Blijf binnen de grenzen!";
            CancelDrag();
            return;
        }

        // PLAATSEN!!
        Instantiate(prefabToPlace, position, Quaternion.identity);

        // Bepaal type voor API (Dog_Dachshund, Toy_Ball, etc.)
        string objectType = DetermineObjectType(prefabToPlace);

        //De informatie naar de server sturen
        StartCoroutine(APIManager.AddObject(
            GameState.SelectedWorldId,
            objectType,
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

    private string DetermineObjectType(GameObject prefab)
    {
        // Vereenvoudigde typebepaling - pas aan op basis van je prefab namen
        if (prefab == dachshundPrefab) return "Dog_Dachshund";
        if (prefab == frenchBulldogPrefab) return "Dog_FrenchBulldog";
        if (prefab == shibaInuPrefab) return "Dog_ShibaInu";
        if (prefab == poodlePrefab) return "Dog_Poodle";
        if (prefab == ballPrefab) return "Toy_Ball";
        if (prefab == bonePrefab) return "Toy_Bone";
        if (prefab == frisbeePrefab) return "Toy_Frisbee";

        return "Unknown";
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