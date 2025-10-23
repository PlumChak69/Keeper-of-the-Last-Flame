using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;           // Player

    [Header("Rig & Camera")]
    public Transform cameraRig;        // rodzic Main Camera
    public Camera cam;

    [Header("Isometric Angle")]
    [Range(10f, 70f)] public float pitch = 35f;
    [Range(0f, 360f)] public float yaw = 45f;

    [Header("Follow")]
    public float followHeight = 20f;
    public float followDistance = 20f;

    [Tooltip("Tightness dla ekspresowego lerpa (wiêksze = szybciej).")]
    public float followTightness = 18f;   // 12–24 zwykle OK

    [Header("Zoom (Ortho)")]
    public float orthoSize = 8f;
    public float minOrtho = 5f;
    public float maxOrtho = 18f;
    public float zoomSpeed = 5f;

    [Header("Rotation Snap (Q/E)")]
    public bool enableSnapRotate = true;
    public float snapStep = 90f;          // skok w stopniach
    public float snapDuration = 0.18f;    // czas animacji skoku

    [Header("Edge Pan (optional)")]
    public bool enableEdgePan = false;
    public int edgePixels = 12;
    public float edgePanSpeed = 12f;

    // wewnêtrzne
    float _yawTarget;
    bool _isSnapping;
    float _snapT;               // 0..1 postêp animacji
    float _yawSnapFrom, _yawSnapTo;
    Vector3 _orbitFrom;         // wektor od targetu do kamery na start snapu (na p³aszczyŸnie XZ)
    float _heightFrom;          // ró¿nica wysokoœci na starcie snapu

    void Reset()
    {
        cam = Camera.main;
        if (!target)
        {
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc) target = cc.transform;
        }
        if (!cameraRig)
        {
            if (cam) cameraRig = cam.transform.parent ? cam.transform.parent : cam.transform;
        }
        _yawTarget = yaw;
        if (cam) cam.orthographic = true;
    }

    void OnValidate()
    {
        if (cam && !cam.orthographic) cam.orthographic = true;
        orthoSize = Mathf.Clamp(orthoSize, minOrtho, maxOrtho);
        if (_yawTarget == 0f) _yawTarget = yaw;
    }

    void LateUpdate()
    {
        if (!target || !cameraRig || !cam) return;

        // --- Zoom rolk¹ ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            orthoSize = Mathf.Clamp(orthoSize - scroll * zoomSpeed, minOrtho, maxOrtho);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, orthoSize, 1f - Mathf.Exp(-10f * Time.deltaTime));

        // --- Start snap-rotacji Q/E ---
        if (enableSnapRotate && Application.isPlaying && !_isSnapping)
        {
            if (Input.GetKeyDown(KeyCode.Q)) BeginSnap(-snapStep);
            if (Input.GetKeyDown(KeyCode.E)) BeginSnap(+snapStep);
        }

        if (_isSnapping)
        {
            // animacja bezdrganiowa: orbitujemy wokó³ celu sta³ym promieniem
            _snapT = Mathf.Clamp01(_snapT + (Time.deltaTime / Mathf.Max(0.0001f, snapDuration)));
            float t = Smooth01(_snapT);

            // interpoluj yaw
            yaw = Mathf.LerpAngle(_yawSnapFrom, _yawSnapTo, t);

            // odtwarzaj wektor orbity (XZ) przez obrót o DeltaYaw
            float deltaYaw = Mathf.DeltaAngle(_yawSnapFrom, yaw);
            Vector3 orbitXZ = Quaternion.Euler(0f, deltaYaw, 0f) * _orbitFrom;

            // wysokoœæ trzymajmy sta³¹ podczas snapa (brak „pompowania” kamery)
            Vector3 desiredPos =
                new Vector3(target.position.x, target.position.y + _heightFrom, target.position.z)
                + orbitXZ;

            cameraRig.position = desiredPos;
            cameraRig.rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (_snapT >= 1f)
            {
                _isSnapping = false;
                // po zakoñczeniu snapa nic nie „doganiamy” — jesteœmy ju¿ na torze
            }
            return; // wa¿ne: nie wykonujemy zwyk³ego follow w tej klatce
        }

        // --- Zwyk³y follow (exp-lerp: szybki, bez „gumienia”) ---
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = rot * Vector3.back;
        Vector3 offset = back * followDistance + Vector3.up * followHeight;

        Vector3 desired = target.position + offset;

        // Edge pan (opcjonalnie)
        if (enableEdgePan && Application.isPlaying)
        {
            Vector2 mouse = Input.mousePosition;
            float w = Screen.width, h = Screen.height;
            Vector3 pan = Vector3.zero;

            if (mouse.x <= edgePixels) pan += (rot * Vector3.left);
            else if (mouse.x >= w - edgePixels) pan += (rot * Vector3.right);
            if (mouse.y <= edgePixels) pan += Vector3.ProjectOnPlane(rot * Vector3.back, Vector3.up);
            else if (mouse.y >= h - edgePixels) pan += Vector3.ProjectOnPlane(rot * Vector3.forward, Vector3.up);

            if (pan.sqrMagnitude > 0.001f)
                desired += Vector3.ProjectOnPlane(pan, Vector3.up).normalized * (edgePanSpeed * Time.deltaTime);
        }

        // ekspresowy lerp (krytycznie t³umiony) — bardzo responsywny
        float alpha = 1f - Mathf.Exp(-followTightness * Time.deltaTime);
        cameraRig.position = Vector3.Lerp(cameraRig.position, desired, alpha);
        cameraRig.rotation = rot;
    }

    void BeginSnap(float delta)
    {
        _isSnapping = true;
        _snapT = 0f;

        _yawSnapFrom = yaw;
        _yawSnapTo = _yawSnapFrom + delta;

        // zapamiêtaj wektor orbity i wysokoœæ wzglêdem celu
        Vector3 from = cameraRig.position - target.position;
        _heightFrom = from.y;
        _orbitFrom = new Vector3(from.x, 0f, from.z);
        if (_orbitFrom.sqrMagnitude < 0.0001f)
        {
            // awaryjnie – ustaw orbitê z aktualnych ustawieñ
            Quaternion rot = Quaternion.Euler(0f, _yawSnapFrom, 0f);
            _orbitFrom = rot * Vector3.back * followDistance;
            _heightFrom = followHeight;
        }
    }

    static float Smooth01(float t)
    {
        // g³adkie S-curve (jak SmoothStep, ale bardziej „miêkka”)
        return t * t * (3f - 2f * t);
    }

    // Pomocnicze dla PlayerMovementIso
    public Vector3 GetCameraForwardOnPlane()
    {
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        return (rot * Vector3.forward).normalized;
    }
    public Vector3 GetCameraRightOnPlane()
    {
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        return (rot * Vector3.right).normalized;
    }
}
