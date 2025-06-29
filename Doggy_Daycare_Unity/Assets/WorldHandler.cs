using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldHandler : MonoBehaviour
{
    /* ────────── Inspector references ────────── */
    [Header("UI")]
    public Transform listParent;        // Content of ScrollView
    public GameObject worldCardPrefab;   // Prefab with Name, OpenButton, DeleteButton

    public TMP_InputField nameField;
    public TMP_InputField widthField;
    public TMP_InputField heightField;

    public Button createButton;
    public Button refreshButton;
    public Button logoutButton;

    public TextMeshProUGUI feedbackText;

    /* ────────── Unity lifecycle ────────── */
    private void Start()
    {
        createButton.onClick.AddListener(CreateWorld);
        Debug.Log("sweet");
        refreshButton.onClick.AddListener(() => StartCoroutine(LoadWorlds()));
        logoutButton.onClick.AddListener(Logout);

        StartCoroutine(LoadWorlds());
    }

    private void Logout()
    {
        APIManager.Logout();
        SceneManager.LoadScene("StartMenu");
    }

    /* ────────── Load & render the user’s worlds ────────── */
    private IEnumerator LoadWorlds()
    {
        feedbackText.text = "";
        yield return APIManager.GetWorlds(apiResponse =>
        {
            /* 1. clear old cards */
            foreach (Transform child in listParent) Destroy(child.gameObject);

            /* 2. check HTTP errors */
            if (!apiResponse.Success)
            {
                feedbackText.text = "Error: " + apiResponse.Message;
                Debug.Log($"GET /worlds → {apiResponse.StatusCode}  {apiResponse.Message}");
                return;
            }

            /* ── SAFE PARSING BLOCK ───────────────────────────────────── */
            string rawJson = string.IsNullOrWhiteSpace(apiResponse.Data) ? "[]" : apiResponse.Data;
            string wrapped = "{\"items\":" + rawJson + "}";
            WorldList parsed = JsonUtility.FromJson<WorldList>(wrapped)
                              ?? new WorldList { items = new APIManager.WorldDto[0] };

            APIManager.WorldDto[] items = parsed.items;
            /* ─────────────────────────────────────────────────────────── */

            /* 3. empty list message */
            if (items == null || items.Length == 0)
            {
                Debug.Log("RAW JSON from /worlds: " + rawJson); // debug aid
                feedbackText.text = "No worlds yet.";
                return;
            }

            /* 4. spawn one card per world */
            foreach (APIManager.WorldDto world in items)
            {
                if (worldCardPrefab == null)
                {
                    Debug.LogError("[WorldHandler] worldCardPrefab is NOT assigned in the Inspector!");
                    break;   // nothing else will work
                }

                // 6-b instantiate the prefab under the Content transform
                GameObject card = Instantiate(worldCardPrefab, listParent);
                Debug.Log($"[DEBUG] Spawned card GO = {card.name}  for world = {world.name}");

                // 6-c find expected child objects
                Transform nameTf = card.transform.Find("Name");
                Transform openTf = card.transform.Find("OpenButton");   // child must be named OpenButton
                Transform deleteTf = card.transform.Find("DeleteButton");

                if (nameTf == null || openTf == null || deleteTf == null)
                {
                    Debug.LogError("[WorldHandler] Child objects missing!  " +
                                   $"Name={nameTf != null}  OpenButton={openTf != null}  DeleteButton={deleteTf != null}");
                    continue;   // skip this card until the prefab hierarchy is fixed
                }

                // 6-d apply text & hook buttons (unchanged)
                nameTf.GetComponent<TextMeshProUGUI>().text = world.name;

                openTf.GetComponent<Button>().onClick
                      .AddListener(() => OpenWorld(world));

                deleteTf.GetComponent<Button>().onClick
                      .AddListener(() => StartCoroutine(APIManager.DeleteWorld(
                          world.id,
                          resp =>
                          {
                              Debug.Log($"DELETE /worlds/{world.id} → {resp.StatusCode}  {resp.Message}");
                              if (resp.Success) Destroy(card);
                              else feedbackText.text = "Delete error: " + resp.Message;
                          })));
            }
        });
    }

    private void OpenWorld(APIManager.WorldDto selectedWorld)
    {
        GameState.SelectedWorldId = selectedWorld.id;
        GameState.SelectedWorldWidth = selectedWorld.width;
        GameState.SelectedWorldHeight = selectedWorld.height;
        SceneManager.LoadScene("Daycare");
    }

    /* ────────── Create a new world ────────── */
    private void CreateWorld()
    {
        if (!int.TryParse(widthField.text, out int width) ||
            !int.TryParse(heightField.text, out int height))
        {
            feedbackText.text = "Width & Height must be numbers";
            return;
        }

        StartCoroutine(APIManager.CreateWorld(
            nameField.text, width, height,
            createResponse =>
            {
                Debug.Log($"POST /worlds → Success={createResponse.Success}  Code={createResponse.StatusCode}  Msg={createResponse.Message}");

                if (createResponse.Success)
                {
                    feedbackText.text = "World created!";
                    StartCoroutine(LoadWorlds());
                    return;
                }

                /* map backend messages */
                string friendly = createResponse.Message;
                if (friendly.Contains("Name length")) friendly = "Name must be 1–25 characters";
                else if (friendly.Contains("Width")) friendly = "Width must be 20–200";
                else if (friendly.Contains("Height")) friendly = "Height must be 10–100";
                else if (friendly.Contains("Max 5")) friendly = "You already have 5 worlds";
                else if (friendly.Contains("already in use")) friendly = "Name already exists";
                else friendly = "Error: " + friendly;

                feedbackText.text = friendly;
            }));
    }

    /* helper DTO so JsonUtility can parse array */
    [System.Serializable]
    private class WorldList
    {
        public APIManager.WorldDto[] items;
    }
}
