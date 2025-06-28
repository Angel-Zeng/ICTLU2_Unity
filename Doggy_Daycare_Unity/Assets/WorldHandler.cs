using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WorldHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform listParent;          // empty VerticalLayoutGroup
    [SerializeField] private GameObject worldCardPrefab;    // prefab with name text + buttons
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private TMP_InputField widthField;
    [SerializeField] private TMP_InputField heightField;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Button createButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button logoutButton;

    private void Awake()
    {
        createButton.onClick.AddListener(CreateWorld);
        refreshButton.onClick.AddListener(() => StartCoroutine(LoadWorlds()));
        logoutButton.onClick.AddListener(() =>
        {
            Api.Logout();
            SceneManager.LoadScene("LoginScene");
        });
    }

    private void Start() => StartCoroutine(LoadWorlds());

    // ???????????????????  Fetch & render list  ??????????????????
    private IEnumerator LoadWorlds()
    {
        foreach (Transform child in listParent) Destroy(child.gameObject);
        feedbackText.text = "Loading...";
        yield return Api.GetWorlds(OnWorldsOk, err => feedbackText.text = err);
    }

    private void OnWorldsOk(Api.WorldDto[] worlds)
    {
        feedbackText.text = worlds.Length == 0 ? "No worlds yet." : "";
        foreach (var w in worlds)
        {
            var go = Instantiate(worldCardPrefab, listParent);
            go.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = w.name;

            // Open button
            go.transform.Find("OpenBtn").GetComponent<Button>()
              .onClick.AddListener(() =>
              {
                  GameState.SelectedWorldId = w.id;
                  GameState.SelectedWorldWidth = w.width;
                  GameState.SelectedWorldHeight = w.height;
                  SceneManager.LoadScene("EditorScene");
              });

            // Delete button
            go.transform.Find("DelBtn").GetComponent<Button>()
              .onClick.AddListener(() =>
                  StartCoroutine(Api.DeleteWorld(w.id,
                      () => { Destroy(go); },      // optimistic UI update
                      e => feedbackText.text = $"Delete failed: {e}")));
        }
    }

    // ???????????????????  Create world  ?????????????????????????
    private void CreateWorld()
    {
        if (!int.TryParse(widthField.text, out int w) ||
            !int.TryParse(heightField.text, out int h))
        {
            feedbackText.text = "Width/Height must be numbers";
            return;
        }

        StartCoroutine(Api.CreateWorld(nameField.text, w, h,
            () =>
            {
                feedbackText.text = "Created!";
                StartCoroutine(LoadWorlds());     // refresh list
            },
            e => feedbackText.text = $"Create failed: {e}"));
    }
}
