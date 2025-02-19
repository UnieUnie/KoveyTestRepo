using System.Collections;
using UnityEngine;
using Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    // Blur Control
    [SerializeField] float unblurValue = 32f;
    [SerializeField] float blurValue = 7f;
    [SerializeField] float blurSpeed = 2.5f;
    [SerializeField] Volume volume;
    DepthOfField dof;
    bool isBlurring = false;

    // Camera Control
    [SerializeField] float rotationSpeed = 150f;
    [SerializeField] float minXRotationAngle = 35f;
    [SerializeField] float maxXRotationAngle = 55f;
    [SerializeField] Transform pivotPoint;
    float currentYRotation;
    float currentXRotation;


    void Awake()
    {
        BlurSetup();
    }

    void Start()
    {
        CameraSetup();
    }

    void Update()
    {
        if (!AdvancedInteractionController.isInteracting && !AdvancedInteractionController.isTransitioning && Input.GetMouseButton(1))
        {
            CameraRotation();
        }
    }

    #region Camera Method

    void CameraSetup()
    {
        currentYRotation = transform.eulerAngles.y;
        currentXRotation = transform.eulerAngles.x;
    }

    void CameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float rotationYAmount = mouseX * rotationSpeed * Time.deltaTime;
        float rotationXAmount = -mouseY * rotationSpeed * Time.deltaTime;

        currentYRotation += rotationYAmount;
        currentXRotation = Mathf.Clamp(currentXRotation + rotationXAmount, minXRotationAngle, maxXRotationAngle);

        if (pivotPoint != null)
        {
            Quaternion rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0);
            Vector3 direction = rotation * Vector3.forward;
            transform.position = pivotPoint.position - direction * Vector3.Distance(transform.position, pivotPoint.position);
            transform.LookAt(pivotPoint);
        }
    }
    #endregion

    #region Blur Method
    void BlurSetup()
    {
        if (volume == null || !volume.profile.TryGet(out dof))
        {
            Debug.LogError("volume for blur is null");
            return;
        }

        dof.aperture.Override(unblurValue);
    }

    public void BlurToggle(bool enabled)
    {
        if (!isBlurring) StartCoroutine(ApplyBlur(enabled));
    }

    IEnumerator ApplyBlur(bool enableBlur)
    {
        isBlurring = true;

        float startValue = dof.aperture.value;
        float targetValue = enableBlur ? blurValue : unblurValue;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * blurSpeed;
            dof.aperture.Override(Mathf.Lerp(startValue, targetValue, t));
            yield return null;
        }

        dof.aperture.Override(targetValue);
        isBlurring = false;
    }
    #endregion
}
