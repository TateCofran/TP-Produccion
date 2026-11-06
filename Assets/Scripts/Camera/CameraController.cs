using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Zoom")]
    [Tooltip("FOV change speed.")]
    public float zoomSpeed = 5f;
    [Tooltip("Minimum FOV (closest).")]
    public float minZoom = 5f;
    [Tooltip("Maximum FOV (farthest).")]
    public float maxZoom = 40f;
    [Tooltip("How fast the FOV reaches the target. Higher is snappier.")]
    [Min(0.01f)] public float zoomSmoothing = 10f;
    [Tooltip("Normalized zoom above which the camera counts as zoomed out (tutorial event).")]
    [Range(0f, 1f)] public float zoomedOutThreshold = 0.8f;

    public event Action<float> OnZoomChanged;

    public float ZoomNormalized => Mathf.Approximately(maxZoom, minZoom) ? 0f : Mathf.InverseLerp(minZoom, maxZoom, currentFov);

    private Camera cam;
    private CinemachineCamera cinemachineCamera;
    private float targetFov;
    private float currentFov;
    private float lastNotifiedZoom = -1f;
    private bool zoomedOut;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        cinemachineCamera = GetComponent<CinemachineCamera>();

        if (cam == null && cinemachineCamera == null)
        {
            Debug.LogWarning("[CameraController] No Camera or CinemachineCamera found. Assign the main camera");
            return;
        }

        currentFov = ReadFov();
        targetFov = Mathf.Clamp(currentFov, minZoom, maxZoom);
        NotifyZoom();
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical);
        Vector3 movement = direction * moveSpeed * Time.deltaTime;

        transform.Translate(movement, Space.World);
    }

    private void HandleZoom()
    {
        if (cam == null && cinemachineCamera == null)
            return;

        float dt = Time.unscaledDeltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f && !IsPointerOverUI())
            targetFov -= scroll * zoomSpeed * 10f;

        if (Input.GetKey(KeyCode.Q))
            targetFov -= zoomSpeed * dt;

        if (Input.GetKey(KeyCode.R))
            targetFov += zoomSpeed * dt;

        targetFov = Mathf.Clamp(targetFov, minZoom, maxZoom);

        float t = 1f - Mathf.Exp(-zoomSmoothing * dt);
        currentFov = Mathf.Lerp(currentFov, targetFov, t);
        if (Mathf.Abs(currentFov - targetFov) < 0.01f) currentFov = targetFov;

        WriteFov(currentFov);
        NotifyZoom();
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private float ReadFov()
    {
        if (cinemachineCamera != null) return cinemachineCamera.Lens.FieldOfView;
        return cam != null ? cam.fieldOfView : minZoom;
    }

    private void WriteFov(float fov)
    {
        if (cinemachineCamera != null)
        {
            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = fov;
            cinemachineCamera.Lens = lens;
        }

        if (cam != null) cam.fieldOfView = fov;
    }

    private void NotifyZoom()
    {
        float z = ZoomNormalized;
        if (Mathf.Abs(z - lastNotifiedZoom) < 0.0005f) return;
        lastNotifiedZoom = z;
        OnZoomChanged?.Invoke(z);

        bool nowZoomedOut = z >= zoomedOutThreshold;
        if (nowZoomedOut && !zoomedOut) TutorialEventHub.RaiseZoomedOut();
        zoomedOut = nowZoomedOut;
    }
}
