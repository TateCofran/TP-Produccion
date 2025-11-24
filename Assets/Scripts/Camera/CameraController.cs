using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Zoom")]
    [Tooltip("Velocidad de cambio de FOV.")]
    public float zoomSpeed = 5f;
    [Tooltip("FOV mínimo (más cerca).")]
    public float minZoom = 5f;
    [Tooltip("FOV máximo (más lejos).")]
    public float maxZoom = 40f;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[CameraController] No se encontró Camera.main. Asigná la cámara principal.");
        }
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
        if (cam == null)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel"); 
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            cam.fieldOfView -= scroll * zoomSpeed * 10f;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            cam.fieldOfView -= zoomSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.R))
        {
            cam.fieldOfView += zoomSpeed * Time.deltaTime;
        }

        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
    }
}
