using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractHintUI : MonoBehaviour
{
    public InteractorIso interactor;
    public Graphic textGraphic; // Text lub TMP_Text (Graphic wystarczy)
    public string hint = "[E] Interakcja";

    void Update()
    {
        if (!interactor || !textGraphic) return;

        bool show = interactor.current != null;
        textGraphic.canvasRenderer.SetAlpha(show ? 1f : 0f);

        if (textGraphic is TMP_Text tmp) tmp.text = hint;
        else if (textGraphic is Text ui) ui.text = hint;
    }
}
