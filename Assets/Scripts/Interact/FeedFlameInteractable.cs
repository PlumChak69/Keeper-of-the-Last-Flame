using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FeedFlameInteractable : MonoBehaviour, IInteractable
{
    public FlameEnergy flame;
    public float amount = 10f;
    public float cooldown = 0.25f;
    float _nextTime;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // nie musi blokowaæ ruchu
        if (!flame) flame = FindFirstObjectByType<FlameEnergy>();
    }

    public void Interact()
    {
        if (Time.time < _nextTime || flame == null) return;
        flame.FeedFlame(amount);
        _nextTime = Time.time + cooldown;
    }
}
