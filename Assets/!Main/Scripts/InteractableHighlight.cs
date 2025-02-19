using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractableHighlight : MonoBehaviour
{
    [SerializeField] Material highlightMaterial;

    Material originalTargetMaterial;
    Transform highlightTarget;

    void Update()
    {
        if (InteractableController.isInteracting || InteractableController.isTransitioning || Input.GetMouseButton(1))
        {
            ResetHighlight();
            return;
        }

        if (highlightTarget)
        {
            ResetHighlight();
        }

        if (!EventSystem.current.IsPointerOverGameObject())
        {
            CastHighlight();
        }
    }

    void CastHighlight()
    {
        // Mouse Raycast
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit)) return;

        // Tag Check
        if (!hit.transform.CompareTag("Interactable")) return;

        // TryGet target renderer, also prevents recurring selection
        if (!hit.transform.TryGetComponent(out MeshRenderer renderer) || renderer.material == highlightMaterial) return;

        // Get target material before changing it
        originalTargetMaterial = renderer.material;

        // Change target object material
        renderer.material = highlightMaterial;
        highlightTarget = hit.transform;
    }

    void ResetHighlight()
    {
        if (highlightTarget == null || originalTargetMaterial == null) return;

        if (!highlightTarget.TryGetComponent(out MeshRenderer renderer)) return;

        // Change target material back to original
        renderer.sharedMaterial = originalTargetMaterial;

        // Clear highlight ref
        highlightTarget = null;
    }
}
