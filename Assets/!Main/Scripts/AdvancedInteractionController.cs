using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AdvancedInteractionController : MonoBehaviour
{
    #region Public Static Interaction States
    public static bool isInteracting = false;   // true when object is being interacted/at the interacting area
    public static bool isTransitioning = false; // True when object is moving back/to the interacting area
    #endregion

    #region Inspector References
    [Header("Camera Controller Reference")]
    [SerializeField, Tooltip("Camera Controller Script, should be where the main camera is.")]
    CameraController cameraController;

    [Header("Highlight Material")]
    [SerializeField, Tooltip("Material to apply for highlighting (when player's mouse hovers over an interactable object.)")]
    Material highlightMaterial;

    [Header("Interaction Target")]
    [SerializeField, Tooltip("The transform/GO reference of where the object is to be during interaction.")]
    Transform targetPositionRef;
    #endregion

    #region Interaction Settings
    [Header("Transition -  Movement / Scale / Rotation Speeds")]
    [SerializeField, Tooltip("Speed at which an object moves during transition")]
    float moveSpeed = 15f;

    [SerializeField, Tooltip("Speed at which interactables scale up/down during transition")]
    float scaleSpeed = 2f;

    [SerializeField, Tooltip("Speed at which interactables rotate during transition")]
    float rotationSpeed = 180f;

    [SerializeField, Tooltip("Scale multiplier while the object is held/interacted with.")]
    float scaleValue = 2f;

    [Header("Interaction - Rotation Speeds")]
    [SerializeField, Tooltip("Speed at which object rotates according to player input")]
    float rotationSensitivity = 1.0f;
    #endregion

    #region Internal Fields
    // Highlight references --------------
    Material originalTargetMaterial;
    Transform highlightTarget;

    // Interaction references ------------
    Transform interactingInteractable;
    Vector3 cachePos;
    Quaternion cacheRot;
    Vector3 cacheScale;
    Vector3 mousePreviousPos;
    #endregion

    private void Awake()
    {
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
            if (cameraController == null)
                Debug.LogError("No CameraController");
        }

        if (targetPositionRef == null)
        {
            Debug.LogError("No targetPositionRef");
        }
    }

    private void Update()
    {
        // Highlight logic (hover over objects)
        HandleHighlight();

        // Interaction logic (grab, return, mouse drag to rotate)
        HandleInteraction();
    }

    #region Highlight Logic
    void HandleHighlight()
    {
        // Highlight only when NOT Interacting, Transitioning or holding right-click
        if (isInteracting || isTransitioning || Input.GetMouseButton(1))
        {
            ResetHighlight();
            return;
        }

        // Ensures only one target is highlighted, reset highlights before casting highlights
        if (highlightTarget != null)
        {
            ResetHighlight();
        }

        // Avoids UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        CastHighlight();
    }

    void CastHighlight()
    {
        // Raycast mouse to scene
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            return;

        // Tag check
        if (!hit.transform.CompareTag("Interactable"))
            return;

        // Ensure target has MeshRenderer and highlight material exists
        if (!hit.transform.TryGetComponent(out MeshRenderer renderer) || renderer.material == highlightMaterial)
            return;

        // Cache original material, then SWAPPPP
        originalTargetMaterial = renderer.material;
        renderer.material = highlightMaterial;
        highlightTarget = hit.transform;
    }

    void ResetHighlight()
    {
        if (!highlightTarget || !originalTargetMaterial)
            return;

        if (highlightTarget.TryGetComponent(out MeshRenderer renderer))
        {
            // Swap back cached original material
            renderer.sharedMaterial = originalTargetMaterial;
        }

        highlightTarget = null;
        originalTargetMaterial = null;
    }
    #endregion

    #region Interaction Logic
    void HandleInteraction()
    {
        // Grab object only when NOT Interacting, Transitioning or holding right-click
        if (Input.GetMouseButtonDown(0) && !isInteracting && !isTransitioning && !Input.GetMouseButton(1))
        {
            GrabInteractable();
        }

        // Return object on ESC or
        if (Input.GetKeyDown(KeyCode.Escape) && isInteracting && !isTransitioning)
        {
            ReturnInteractable();
        }

        // Rotate object only when holding left-click and Interacting. NOT while Transitioning.
        if (isInteracting && !isTransitioning && Input.GetMouseButton(0))
        {
            RotateInteractable();
        }
    }

    void GrabInteractable()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform.CompareTag("Interactable"))
        {
            interactingInteractable = hit.transform;

            // Cache original transform
            cachePos = interactingInteractable.position;
            cacheRot = interactingInteractable.rotation;
            cacheScale = interactingInteractable.localScale;

            isTransitioning = true;
            StartCoroutine(MoveInteractable(
                targetPositionRef.position,
                targetPositionRef.rotation,
                cacheScale * scaleValue,
                true
            ));

            // Call to blur
            cameraController?.BlurToggle(true);
        }
    }

    void ReturnInteractable()
    {
        isTransitioning = true;
        StartCoroutine(MoveInteractable(
            cachePos,
            cacheRot,
            cacheScale,
            false
        ));

        // Call to remove blur
        cameraController?.BlurToggle(false);
    }

    IEnumerator MoveInteractable(Vector3 targetPos, Quaternion targetRot, Vector3 targetScale, bool interactingFlag)
    {
        while (Vector3.Distance(interactingInteractable.position, targetPos) > 0.01f || Quaternion.Angle(interactingInteractable.rotation, targetRot) > 1f || Vector3.Distance(interactingInteractable.localScale, targetScale) > 0.01f)
        {
            // Smooth move to position
            Vector3 nextPosition = Vector3.MoveTowards(interactingInteractable.position, targetPos, moveSpeed * Time.deltaTime);

            // Smooth rotation
            Quaternion nextRotation = Quaternion.RotateTowards(interactingInteractable.rotation, targetRot, rotationSpeed * Time.deltaTime);

            // Smooth scale
            Vector3 nextScale = Vector3.MoveTowards(interactingInteractable.localScale, targetScale,scaleSpeed * Time.deltaTime);

            interactingInteractable.SetPositionAndRotation(nextPosition, nextRotation);
            interactingInteractable.localScale = nextScale;

            yield return null;
        }

        // Snap to final transform
        interactingInteractable.SetPositionAndRotation(targetPos, targetRot);
        interactingInteractable.localScale = targetScale;

        // Update flags
        isInteracting = interactingFlag;
        isTransitioning = false;
    }

    void RotateInteractable()
    {
        if (interactingInteractable == null)
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Rotate around the camera's 'up' for horizontal dragging
        Quaternion rotationY = Quaternion.AngleAxis(-mouseX * rotationSensitivity, Camera.main.transform.up);

        // Rotate around the camera's 'right' for vertical dragging
        Quaternion rotationX = Quaternion.AngleAxis(mouseY * rotationSensitivity, Camera.main.transform.right);

        // Multiply these rotations with the object's current rotation
        interactingInteractable.rotation = rotationY * rotationX * interactingInteractable.rotation;
    }
    #endregion
}