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

}