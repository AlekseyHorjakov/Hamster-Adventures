using UnityEngine;

/// <summary>Двигает фоновый слой с запаздыванием относительно камеры.</summary>
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    private Transform cameraTransform;
    private Vector3 initialCameraPosition;
    private Vector3 initialPosition;
    private float horizontalFactor;
    private float verticalFactor;

    public void Initialize(Transform targetCamera, float horizontal, float vertical)
    {
        cameraTransform = targetCamera;
        horizontalFactor = horizontal;
        verticalFactor = vertical;
        initialCameraPosition = cameraTransform.position;
        initialPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 cameraDelta = cameraTransform.position - initialCameraPosition;
        transform.position = initialPosition + new Vector3(
            cameraDelta.x * horizontalFactor,
            cameraDelta.y * verticalFactor,
            0f);
    }
}
