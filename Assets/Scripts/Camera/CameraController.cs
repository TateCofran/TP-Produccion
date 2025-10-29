using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal"); // A(-1), D(1)
        float vertical = Input.GetAxis("Vertical"); // S(-1), W(1)

        Vector3 direction = new Vector3(horizontal, 0, vertical);
        Vector3 movement = direction * moveSpeed * Time.deltaTime;

        transform.Translate(movement, Space.World);
    }

    void HandleZoom()
    {
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