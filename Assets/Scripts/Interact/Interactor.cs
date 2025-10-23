using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    public Camera cam;                  // przypnij Main Camera
    public float distance = 3f;
    public LayerMask interactMask = ~0; // mo¿esz ograniczyæ do warstw interaktywnych
    public KeyCode key = KeyCode.E;

    void Reset() { cam = Camera.main; }

    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, distance, interactMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.TryGetComponent<IInteractable>(out var ia))
                {
                    ia.Interact();
                }
            }
        }
    }
}
