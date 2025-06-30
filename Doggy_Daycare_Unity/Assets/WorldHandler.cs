using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldHandler : MonoBehaviour
{
    public Transform listParent;

    public GameObject worldCardPrefab;

    public TMP_InputField nameField;
    public TMP_InputField widthField;
    public TMP_InputField heightField;

    public Button createButton;
    public Button refreshButton;
    public Button logoutButton;

    public TextMeshProUGUI feedbackText;

    private void Start()
    {
        //Koppelen van create, refresh en logout knoppen aan hun handlers
        createButton.onClick.AddListener(CreateWorld);
        refreshButton.onClick.AddListener(() => StartCoroutine(LoadWorlds()));
        logoutButton.onClick.AddListener(Logout);

        //Laden van werelden
        StartCoroutine(LoadWorlds());
    }

    //Wanneer uitloggen terug naar het startmenu
    private void Logout()
    {
        APIManager.Logout();
        SceneManager.LoadScene("StartMenu");
    }

    // laden van werelden en in de scrollview weergeven
    private IEnumerator LoadWorlds()
    {
        feedbackText.text = "";
        yield return APIManager.GetWorlds(apiResponse =>
        {
            // De lijst leegmaken voor nieuwe werelden
            foreach (Transform child in listParent) Destroy(child.gameObject);

            //Controleren op fouten in de responses
            if (!apiResponse.Success)
            {
                feedbackText.text = "Oops: " + apiResponse.Message;
                return;
            }

            //Deze moest ik parsen zei Chris want ik kan niet direct in een array zetten
            string rawJson = string.IsNullOrWhiteSpace(apiResponse.Data) ? "[]" : apiResponse.Data;
            string wrapped = "{\"items\":" + rawJson + "}";
            WorldList parsed = JsonUtility.FromJson<WorldList>(wrapped)
                              ?? new WorldList { items = new APIManager.WorldDto[0] };

            APIManager.WorldDto[] items = parsed.items;

            // Als items null of leeg zijn, geen werelden gevonden
            if (items == null || items.Length == 0)
            {
                feedbackText.text = "Nog geen werelden.";
                return;
            }

            //Worldcard prefab voor elke wereld maken
            foreach (APIManager.WorldDto world in items)
            {
                if (worldCardPrefab == null)
                {
                    Debug.LogError("worldCardPrefab is NOT assigned!");
                    break;
                }

                GameObject card = Instantiate(worldCardPrefab, listParent);

                //dit zijn de child objecten die in de worldcard prefab moeten zitten want die moet bij ekle wereld specifiek zijn
                Transform nameTf = card.transform.Find("Name");
                Transform openTf = card.transform.Find("OpenButton");
                Transform deleteTf = card.transform.Find("DeleteButton");

                if (nameTf == null || openTf == null || deleteTf == null)
                {
                    Debug.LogError("KIND MIST");
                    continue;
                }
                nameTf.GetComponent<TextMeshProUGUI>().text = world.name;

                openTf.GetComponent<Button>().onClick
                      .AddListener(() => OpenWorld(world));

                deleteTf.GetComponent<Button>().onClick
                      .AddListener(() => StartCoroutine(APIManager.DeleteWorld(
                          world.id,
                          resp =>
                          {
                              if (resp.Success) Destroy(card);
                              else feedbackText.text = "Delete error: " + resp.Message;
                          })));
            }
        });
    }

    // Openen van de geselectereerde wereld 
    private void OpenWorld(APIManager.WorldDto selectedWorld)
    {
        //gegevens opslaan in de gamestate dtos
        GameState.SelectedWorldId = selectedWorld.id;
        GameState.SelectedWorldWidth = selectedWorld.width;
        GameState.SelectedWorldHeight = selectedWorld.height;

        //Hier gaat ie naar de doggie daycare!!
        SceneManager.LoadScene("Daycare");
    }
    private void CreateWorld()
    {
        //Parsen om te kijken of de velden niet leeg zijn of niet gewoon letters zijn
        if (!int.TryParse(widthField.text, out int width) ||
            !int.TryParse(heightField.text, out int height))
        {
            feedbackText.text = "Width en height moeten nummers bevatten.";
            return;
        }

        //Callen van API om wereld te maken 
        StartCoroutine(APIManager.CreateWorld(
            nameField.text, width, height,
            createResponse =>
            {
                if (createResponse.Success)
                {
                    feedbackText.text = "World created!";
                    StartCoroutine(LoadWorlds()); //deze moet erin om de nieuwe werelden te laden
                }
                else
                {
                    //SOrry dit is heel lelijk ik had een case statement moeten gebruiken!!
                    string friendly = createResponse.Message;
                    if (friendly.Contains("Name length")) friendly = "Naam moet tussen de 1-25 karakters zijn.";
                    else if (friendly.Contains("Width")) friendly = "Width moet tussen de 20-200 zitten";
                    else if (friendly.Contains("Height")) friendly = "Height moet tussen de 10-100 zitten";
                    else if (friendly.Contains("Max 5")) friendly = "Je hebt al 5 werelden!";
                    else if (friendly.Contains("already in use")) friendly = "Naam bestaat al! Kies iets anders!";

                    feedbackText.text = friendly;
                }
            }));
    }

    //Nogmaals dit had in een aparte class gemoeten :)
    [System.Serializable]
    private class WorldList
    {
        public APIManager.WorldDto[] items;
    }
}