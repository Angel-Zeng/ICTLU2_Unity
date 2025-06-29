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
        //it checks if a prefab is selected and if the mouse button is clicked
        if (currentPrefab == null) return;
        if (!Input.GetMouseButtonDown(0)) return;

        //Get the world position of the mouse click and ignores z axis cuz it aint 3d
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

        //spawns the dog or toy at the location
        Instantiate(currentPrefab, worldPosition, Quaternion.identity);

        // Sends a request to the API to add the object
        string typeName = currentPrefab.name;
        StartCoroutine(APIManager.AddObject(
            GameState.SelectedWorldId, typeName, worldPosition.x, worldPosition.y,
            result => feedbackText.text = result.Success ? "Placed" : "Error: " + result.Message));
    }
}