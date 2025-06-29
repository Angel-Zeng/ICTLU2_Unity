using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectHandler : MonoBehaviour
{
    public GameObject dogPrefab;
    public GameObject toyPrefab;

    public TextMeshProUGUI worldNameText;
    public TextMeshProUGUI feedbackText;
    public LineRenderer borderLine;   // empty GO with LineRenderer
    public RectTransform trashZone;    // Image on Canvas named “TrashZone”

    /* ───── internal drag state ───── */
    private GameObject prefabToPlace;   // prefab we’re dragging
    private GameObject ghost;           // semi-transparent preview

    /* ══════════ Scene entry ══════════ */
    private void Start()
    {
        StartCoroutine(LoadWorldThenEnableDrag());
    }

    private IEnumerator LoadWorldThenEnableDrag()
    {
        /* ─── GUARD: came here through “Open” ? ─── */
        if (GameState.SelectedWorldId == 0)
        {
            feedbackText.text = "No world selected – go through World menu.";
            Debug.LogError("Daycare opened with SelectedWorldId = 0");
            yield break;
        }

        int wid = GameState.SelectedWorldId;

        yield return APIManager.GetWorldObjects(wid, data =>
        {
            /* 1) title */
            worldNameText.text =
                $"{data.world.name}";

            /* 2) border */
            DrawBorder(data.world.width, data.world.height);

            /* 3) already-saved objects */
            foreach (var obj in data.objects)
            {
                GameObject prefab = obj.type == "Dog" ? dogPrefab : toyPrefab;
                Instantiate(prefab,
                           new Vector3(obj.x, obj.y, 0),
                           Quaternion.identity);
            }

            feedbackText.text = "Drag a dog or toy!";
        });
    }

    private void DrawBorder(int w, int h)
    {
        borderLine.positionCount = 5;
        borderLine.SetPositions(new Vector3[]
        {
            new(0,0), new(w,0), new(w,h), new(0,h), new(0,0)
        });
    }

    /* ══════════ Palette buttons ══════════ */
    public void BeginDragDog() => BeginDrag(dogPrefab);
    public void BeginDragToy() => BeginDrag(toyPrefab);

    private void BeginDrag(GameObject prefab)
    {
        CancelDrag();                 // cancel any previous drag
        prefabToPlace = prefab;

        ghost = Instantiate(prefabToPlace);
        ghost.name = "Ghost";
        SetAlpha(ghost, 0.45f);
    }

    /* ══════════ Main Update loop ══════════ */
    private void Update()
    {
        if (ghost == null) return;

        /* follow cursor */
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;
        ghost.transform.position = mouse;

        /* LMB released → drop */
        if (Input.GetMouseButtonUp(0))
        {
            bool inTrash = RectTransformUtility.RectangleContainsScreenPoint(
                               trashZone, Input.mousePosition);
            bool inBounds =
                mouse.x >= 0 && mouse.y >= 0 &&
                mouse.x <= GameState.SelectedWorldWidth &&
                mouse.y <= GameState.SelectedWorldHeight;

            if (inTrash)
            {
                feedbackText.text = "Cancelled.";
                CancelDrag();
                return;
            }

            if (!inBounds)
            {
                feedbackText.text = "Stay within bounds!";
                CancelDrag();
                return;
            }

            /* place permanently */
            Instantiate(prefabToPlace, mouse, Quaternion.identity);

            StartCoroutine(APIManager.AddObject(
                GameState.SelectedWorldId,
                prefabToPlace == dogPrefab ? "Dog" : "Toy",
                mouse.x, mouse.y,
                res => feedbackText.text = res.Success
                     ? "Placed!" : "Error: " + res.Message));

            CancelDrag();
        }

        /* RMB cancels */
        if (Input.GetMouseButtonDown(1))
            CancelDrag();
    }

    /* ══════════ Helpers ══════════ */
    private void CancelDrag()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        prefabToPlace = null;
    }

    private static void SetAlpha(GameObject go, float a)
    {
        if (go.TryGetComponent<SpriteRenderer>(out var sr))
        {
            Color c = sr.color; c.a = a; sr.color = c;
        }
    }

    /* ══════════ DTO for /worlds/{id} ══════════ */
    [System.Serializable]
    public class WorldWithObjects
    {
        public APIManager.WorldDto world;
        public ObjectDto[] objects;
    }
    [System.Serializable]
    public class ObjectDto
    {
        public int id;
        public string type;
        public float x;
        public float y;
    }
}
