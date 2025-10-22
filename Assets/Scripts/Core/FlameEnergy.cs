using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameEnergy : MonoBehaviour
{
    [Header("Energy")]
    [Range(0, 200f)] public float energy = 100f;
    public float maxEnergy = 100f;

    [Header("Decay (per second)")]
    public float decayRateDay = 0.5f;   // ile energii traci dziennie (sekunda)
    public float decayRateNight = 1.0f; // ile energii traci noc¹ (sekunda)

    [Header("State (set externally)")]
    public bool isNight = false;        // na razie ustawiamy rêcznie w Inspectorze

    public System.Action OnFlameExtinguished; // event na przysz³oœæ (Game Over)

    public void FeedFlame(float amount)
    {
        energy = Mathf.Clamp(energy + amount, 0f, maxEnergy);
        Debug.Log($"[Flame] Feed +{amount}, energy = {energy:0.0}");
    }

    void Update()
    {
        float rate = isNight ? decayRateNight : decayRateDay;
        energy = Mathf.Max(0f, energy - rate * Time.deltaTime);

        if (energy <= 0f)
        {
            Debug.Log("[Flame] GAME OVER — p³omieñ zgas³.");
            OnFlameExtinguished?.Invoke();
            enabled = false; // tymczasowo zatrzymujemy spadek
        }
    }
}

