using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlameUI : MonoBehaviour
{
    public FlameEnergy flame;
    public Image bar;             // Image typu Filled
    public Gradient colorByEnergy;

    void Update()
    {
        float t = flame.energy / flame.maxEnergy;
        bar.fillAmount = t;
        if (colorByEnergy != null) bar.color = colorByEnergy.Evaluate(t);
    }
}
