using TMPro;
using UnityEngine;

public class ObjectHandler : MonoBehaviour
{
    public GameObject dogPrefab;
    public GameObject toyPrefab;

    public TextMeshProUGUI feedbackText;

    private GameObject currentPrefab;

    public void PickDog() => currentPrefab = dogPrefab;
    public void PickToy() => currentPrefab = toyPrefab;

    private void Update()
    {
        if (currentPrefab == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0;

        // stay inside world size
        if (worldPosition.x < 0 || worldPosition.y < 0 ||
            worldPosition.x > GameState.SelectedWorldWidth ||
            worldPosition.y > GameState.SelectedWorldHeight)
        {
            feedbackText.text = "Stay within bounds!";
            return;
        }

        Instantiate(currentPrefab, worldPosition, Quaternion.identity);

        string typeName = currentPrefab.name;
        StartCoroutine(APIManager.AddObject(
            GameState.SelectedWorldId, typeName, worldPosition.x, worldPosition.y,
            result => feedbackText.text = result.Success ? "Placed" : "Error: " + result.Message));
    }
}