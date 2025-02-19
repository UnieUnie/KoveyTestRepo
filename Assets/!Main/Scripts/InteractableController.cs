using System.Collections;
using UnityEngine;

public class InteractableController : MonoBehaviour
{
    CameraController cameraController;

    Transform interactingInteractable;

    [SerializeField, Tooltip("True when object is being interacted")]
    public static bool isInteracting = false;
    [SerializeField, Tooltip("True when Object is transitioning between interactions")]
    public static bool isTransitioning = false;

    Vector3 cachePos;
    Quaternion cacheRot;
    Vector3 cacheScale;
    Vector3 mousePreviousPos;

    [SerializeField] Transform targetPositionRef;
    [SerializeField] float moveSpeed = 15f;
    [SerializeField] float scaleSpeed = 2f;
    [SerializeField] float rotationSpeed = 180f;
    [SerializeField] float rotationSensitivity = 0.2f;
    [SerializeField] float scaleValue = 2f;

    void Awake()
    {
        cameraController = FindObjectOfType<CameraController>();

        if (targetPositionRef == null)
        {
            Debug.LogError("Interactable Controller targetPositionRef is null");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isInteracting && !isTransitioning && !Input.GetMouseButton(1))
        {
            GrabInteractable();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isInteracting && !isTransitioning)
        {
            ReturnInteractable();
        }

        if (Input.GetMouseButton(0) && isInteracting && !isTransitioning)
            RotateInteractable();
    }

    #region Grab & Return
    void GrabInteractable()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform.CompareTag("Interactable"))
        {
            interactingInteractable = hit.transform;
            cachePos = interactingInteractable.position;
            cacheRot = interactingInteractable.rotation;
            cacheScale = interactingInteractable.localScale;

            isTransitioning = true;
            StartCoroutine(MoveInteractable(targetPositionRef.position, targetPositionRef.rotation, cacheScale * scaleValue, true));
            cameraController.BlurToggle(true);
        }
    }

    void ReturnInteractable()
    {
        isTransitioning = true;
        StartCoroutine(MoveInteractable(cachePos, cacheRot, cacheScale, false));
        cameraController.BlurToggle(false);
    }

    IEnumerator MoveInteractable(Vector3 targetPos, Quaternion targetRot, Vector3 targetScale, bool interactingFlag)
    {
        while (Vector3.Distance(interactingInteractable.position, targetPos) > 0.01f || Quaternion.Angle(interactingInteractable.rotation, targetRot) > 1f || Vector3.Distance(interactingInteractable.localScale, targetScale) > 0.01f)
        {
            Vector3 nextPosition = Vector3.MoveTowards(interactingInteractable.position, targetPos, moveSpeed * Time.deltaTime);
            Quaternion nextRotation = Quaternion.RotateTowards(interactingInteractable.rotation, targetRot, rotationSpeed * Time.deltaTime);
            Vector3 nextScale = Vector3.MoveTowards(interactingInteractable.localScale, targetScale, scaleSpeed * Time.deltaTime);

            interactingInteractable.SetPositionAndRotation(nextPosition, nextRotation);
            interactingInteractable.localScale = nextScale;

            yield return null;
        }

        interactingInteractable.SetPositionAndRotation(targetPos, targetRot);
        interactingInteractable.localScale = targetScale;

        isInteracting = interactingFlag;
        isTransitioning = false;
    }
    #endregion

    void RotateInteractable()
    {
        if (interactingInteractable == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            mousePreviousPos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 mouseOffset = Input.mousePosition - mousePreviousPos;
            Vector3 rotation = new Vector3(mouseOffset.y, -mouseOffset.x, 0f) * rotationSensitivity;
            interactingInteractable.Rotate(rotation, Space.Self);
            mousePreviousPos = Input.mousePosition;
        }
    }
}
