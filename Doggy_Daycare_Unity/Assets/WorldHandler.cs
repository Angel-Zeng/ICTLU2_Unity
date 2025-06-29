using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldHandler : MonoBehaviour
{
    /* ────────── Inspector references ────────── */
    [Header("UI")]
    public Transform listParent;          // VerticalLayoutGroup inside ScrollView
    public GameObject worldCardPrefab;     // prefab with Name, OpenButton, DeleteButton

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
            /* 1.  Clear existing cards */
            foreach (Transform child in listParent) Destroy(child.gameObject);

            /* 2.  If request failed, show error & exit */
            if (!apiResponse.Success)
            {
                feedbackText.text = "Error: " + apiResponse.Message;
                Debug.Log($"GET /worlds → {apiResponse.StatusCode}  {apiResponse.Message}");
                return;
            }

            /* 3.  Ensure we always parse something */
            string rawJson = string.IsNullOrWhiteSpace(apiResponse.Data) ? "[]" : apiResponse.Data;

            /* 4.  Wrap array so JsonUtility can parse it */
            string wrappedJson = "{\"items\":" + rawJson + "}";
            WorldList worldList = JsonUtility.FromJson<WorldList>(wrappedJson)
                                 ?? new WorldList { items = new APIManager.WorldDto[0] };

            /* 5.  Show “No worlds yet.” if list empty */
            if (worldList.items == null || worldList.items.Length == 0)
            {
                feedbackText.text = "No worlds yet.";
                return;
            }

            /* 6.  Create a card for each world */
            foreach (APIManager.WorldDto worldEntry in worldList.items)
            {
                GameObject card = Instantiate(worldCardPrefab, listParent);
                card.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = worldEntry.name;

                // OPEN
                card.transform.Find("CreateButton").GetComponent<Button>()
                     .onClick.AddListener(() => OpenWorld(worldEntry));

                // DELETE
                card.transform.Find("DeleteButton").GetComponent<Button>()
                     .onClick.AddListener(() => StartCoroutine(APIManager.DeleteWorld(
                         worldEntry.id,
                         deleteResponse =>
                         {
                             Debug.Log($"DELETE /worlds/{worldEntry.id} → {deleteResponse.StatusCode}  {deleteResponse.Message}");
                             if (deleteResponse.Success) Destroy(card);
                             else feedbackText.text = "Delete error: " + deleteResponse.Message;
                         })));
            }
        });
    }

    private void OpenWorld(APIManager.WorldDto selectedWorld)
    {
        GameState.SelectedWorldId = selectedWorld.id;
        GameState.SelectedWorldWidth = selectedWorld.width;
        GameState.SelectedWorldHeight = selectedWorld.height;
        SceneManager.LoadScene("Daycare");      // your editor scene
    }

    /* ────────── Create a new world ────────── */
    private void CreateWorld()
    {
        /* Quick client-side numeric guard */
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

                /* Map backend 400-messages to friendly UI text */
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

    /* ────────── Helper DTO wrapper for JSON parsing ────────── */
    [System.Serializable]
    private class WorldList
    {
        public APIManager.WorldDto[] items;
    }
}
