using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;       // zwykle CameraPivot (dziecko Playera)
    public Transform playerBody;   // root gracza (do obracania yaw)

    [Header("Orbit")]
    public float yawSensitivity = 180f;   // deg/s przy ruchu myszy
    public float pitchSensitivity = 180f;
    public float minPitch = -35f;
    public float maxPitch = 70f;
    public bool lockCursor = true;

    [Header("Distance & Zoom")]
    public float distance = 4f;
    public float minDistance = 1.5f;
    public float maxDistance = 6f;
    public float zoomSpeed = 3f;

    [Header("Collision")]
    public float cameraRadius = 0.2f;     // „gruboœæ” kamery do spherecastu
    public LayerMask collisionMask = ~0;  // co traktowaæ jako przeszkody
    public float collisionOffset = 0.05f; // odsuniêcie od œciany

    [Header("Smoothing")]
    public float followSmooth = 12f;      // pod¹¿anie kamery
    public float aimSmooth = 20f;         // wyg³adzanie rotacji

    [Header("Shoulder / Offset")]
    public Vector3 pivotOffset = new Vector3(0.4f, 1.6f, 0f); // przesuniêcie „na ramiê”

    // internal
    Camera cam;
    float yaw;      // obrót poziomy kamery (wokó³ gracza)
    float pitch;    // obrót pionowy kamery
    float targetDistance;
    Vector3 currentPivotPos;

    void Awake()
    {
        cam = GetComponent<Camera>();
        targetDistance = distance;
        if (lockCursor && Application.isPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1) Odczyt myszy (delta * sens * dt)
        float mx = Input.GetAxis("Mouse X") * yawSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * pitchSensitivity * Time.deltaTime;
        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 2) Zoom rolk¹
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        // 3) Pozycja pivotu (target + offset) z lekkim smoothingiem
        Vector3 desiredPivot = target.position + target.TransformVector(pivotOffset);
        currentPivotPos = Vector3.Lerp(currentPivotPos == default ? desiredPivot : currentPivotPos, desiredPivot, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));

        // 4) Oblicz docelow¹ pozycjê kamery wzglêdem pivotu
        Quaternion orbitRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredCamPos = currentPivotPos - (orbitRot * Vector3.forward) * targetDistance;

        // 5) Kolizje – spherecast od pivotu do docelowej pozycji
        Vector3 dir = (desiredCamPos - currentPivotPos);
        float desiredLen = dir.magnitude;
        dir = desiredLen > 0.0001f ? dir / desiredLen : Vector3.back;

        float finalLen = desiredLen;
        if (Physics.SphereCast(currentPivotPos, cameraRadius, dir, out RaycastHit hit, desiredLen, collisionMask, QueryTriggerInteraction.Ignore))
        {
            finalLen = Mathf.Max(minDistance, hit.distance - collisionOffset);
        }
        Vector3 finalCamPos = currentPivotPos + dir * finalLen;

        // 6) Ustaw rotacjê kamery i jej pozycjê (z wyg³adzeniem)
        transform.position = Vector3.Lerp(transform.position, finalCamPos, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, orbitRot, 1f - Mathf.Exp(-aimSmooth * Time.deltaTime));

        // 7) Obrót korpusu gracza za kamer¹ (opcjonalnie)
        if (playerBody != null)
        {
            // obracaj tylko yawem (poziomo)
            Quaternion bodyRot = Quaternion.Euler(0f, yaw, 0f);
            playerBody.rotation = Quaternion.Slerp(playerBody.rotation, bodyRot, 1f - Mathf.Exp(-aimSmooth * Time.deltaTime));
        }
    }
}
