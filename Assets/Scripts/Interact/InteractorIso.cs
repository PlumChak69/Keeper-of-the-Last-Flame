using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractorIso : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // Main Camera (izometryczna)
    public Transform player;           // root gracza (z CharacterController)

    [Header("Interaction")]
    public float interactRange = 3.5f; // zasiêg z brzegu collidera (ClosestPoint)
    public LayerMask interactMask;     // np. warstwa "Interactable"
    public KeyCode key = KeyCode.E;
    public bool clickToInteract = true;

    [Header("Debug")]
    public bool drawGizmos = true;

    // runtime
    public IInteractable current;      // aktualnie „pod celownikiem”
    Collider currentCol;

    void Reset()
    {
        cam = Camera.main;
        if (!player)
        {
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc) player = cc.transform;
        }
        interactMask = ~0; // domyœlnie wszystko
    }

    void Update()
    {
        current = null;
        currentCol = null;
        if (!cam || !player) return;

        // Ray spod kursora
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, interactMask, QueryTriggerInteraction.Collide))
        {
            // Szukamy komponentu interakcji na trafionym obiekcie lub jego rodzicu
            if (hit.collider.TryGetComponent<IInteractable>(out var ia) ||
                hit.collider.GetComponentInParent<IInteractable>() != null)
            {
                if (ia == null) ia = hit.collider.GetComponentInParent<IInteractable>();

                // dystans mierzony do najbli¿szego punktu collidra (nie do œrodka)
                Vector3 closest = hit.collider.ClosestPoint(player.position);
                float dist = Vector3.Distance(player.position, closest);

                if (dist <= interactRange)
                {
                    current = ia;
                    currentCol = hit.collider;

                    // Wywo³anie interakcji
                    bool pressed = Input.GetKeyDown(key) || (clickToInteract && Input.GetMouseButtonDown(0));
                    if (pressed)
                    {
                        ia.Interact(); // korzysta z Twojego istniej¹cego interfejsu
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !player) return;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(player.position, interactRange);
        if (currentCol)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.35f);
            Gizmos.DrawWireCube(currentCol.bounds.center, currentCol.bounds.size);
        }
    }
}
