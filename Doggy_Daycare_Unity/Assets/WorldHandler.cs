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


    //Here it uses the AddListener(built in Unity)method to bind the button clicks to the respective methods. 
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
        SceneManager.LoadScene("LoginScene");
    }

    // Here it loads the worlds from the backend API and gives UI feedback.
    private IEnumerator LoadWorlds()
    {
        feedbackText.text = "Loading...";
        yield return APIManager.GetWorlds(result =>
        {
            foreach (Transform card in listParent) Destroy(card.gameObject);

            if (!result.Success)
            {
                feedbackText.text = "Error: " + result.Message;
                return;
            }

            // JsonUtility can’t parse bare arrays, so I wrapped it 
            string wrapped = "{\"items\":" + result.Data + "}";
            WorldList temporary = JsonUtility.FromJson<WorldList>(wrapped);

            feedbackText.text = temporary.items.Length == 0 ? "No worlds yet." : "";

            foreach (var world in temporary.items)
            {
                GameObject card = Instantiate(worldCardPrefab, listParent);
                card.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = world.name;

                // OPEN
                card.transform.Find("OpenButton").GetComponent<Button>().onClick
                    .AddListener(() => OpenWorld(world));

                // DELETE
                card.transform.Find("DeleteButton").GetComponent<Button>().onClick
                    .AddListener(() => StartCoroutine(APIManager.DeleteWorld(
                        world.id,
                        delResult =>
                        {
                            if (delResult.Success) Destroy(card);
                            else feedbackText.text = "Delete error: " + delResult.Message;
                        })));
            }
        });
    }

    private void OpenWorld(APIManager.WorldDto world)
    {
        GameState.SelectedWorldId = world.id;
        GameState.SelectedWorldWidth = world.width;
        GameState.SelectedWorldHeight = world.height;
        SceneManager.LoadScene("EditorScene");
    }

    //Here this method is called when user clicks to create a world.
    private void CreateWorld()
    {
        if (!int.TryParse(widthField.text, out int width) ||
            !int.TryParse(heightField.text, out int height))
        {
            feedbackText.text = "Width/Height must be numbers";
            return;
        }

        StartCoroutine(APIManager.CreateWorld(
            nameField.text, width, height,
            result =>
            {
                feedbackText.text = result.Success ? "Created!" : "Error: " + result.Message;
                if (result.Success) StartCoroutine(LoadWorlds());
            }));
    }



    // to wrap the worlds array in a class for JSON parsing. 
    [System.Serializable]
    private class WorldList
    {
        public APIManager.WorldDto[] items;
    }
}

