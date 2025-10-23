using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SunMoonController : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle cycle;       // przypnij swój DayNightCycle
    public Light sunLight;            // Directional Light
    public Camera cam;                // Main Camera (opcjonalnie)

    [Header("Objects (billboards)")]
    public Transform sunSprite;       // Quad/Plane z materia³em S³oñca
    public Transform moonSprite;      // Quad/Plane z materia³em Ksiê¿yca

    [Header("Placement")]
    public float skyDistance = 500f;  // jak daleko od kamery „na niebie”
    public float sunSize = 20f;       // skala sprite'a
    public float moonSize = 16f;

    [Header("Fading")]
    [Tooltip("Krzywa widocznoœci S³oñca w funkcji czasu doby (0..1)")]
    public AnimationCurve sunOpacityOverDay = AnimationCurve.Linear(0, 0, 1, 0);
    [Tooltip("Krzywa widocznoœci Ksiê¿yca (0..1)")]
    public AnimationCurve moonOpacityOverDay = AnimationCurve.Linear(0, 1, 1, 1);

    Material _sunMat, _moonMat;
    Billboard _sunBB, _moonBB;

    void Reset()
    {
        if (!cycle) cycle = FindFirstObjectByType<DayNightCycle>();
        if (!sunLight) sunLight = RenderSettings.sun;
        if (!cam) cam = Camera.main;
    }

    void OnEnable()
    {
        CacheMaterials();
        EnsureBillboards();
    }

    void CacheMaterials()
    {
        if (sunSprite && sunSprite.TryGetComponent(out Renderer sr))
            _sunMat = sr.sharedMaterial;
        if (moonSprite && moonSprite.TryGetComponent(out Renderer mr))
            _moonMat = mr.sharedMaterial;
    }

    void EnsureBillboards()
    {
        if (sunSprite && !sunSprite.GetComponent<Billboard>())
            _sunBB = sunSprite.gameObject.AddComponent<Billboard>();
        if (moonSprite && !moonSprite.GetComponent<Billboard>())
            _moonBB = moonSprite.gameObject.AddComponent<Billboard>();

        if (_sunBB) _sunBB.cam = cam;
        if (_moonBB) _moonBB.cam = cam;
    }

    void LateUpdate()
    {
        if (!cam || !sunLight) { Reset(); return; }

        // kierunki: S³oñce = przeciwny do forward œwiat³a; Ksiê¿yc = wzd³u¿ forward
        Vector3 sunDir = -sunLight.transform.forward;
        Vector3 moonDir = sunLight.transform.forward;

        // pozycja na „kopule nieba” wzglêdem kamery
        if (sunSprite)
        {
            sunSprite.position = cam.transform.position + sunDir * skyDistance;
            sunSprite.localScale = Vector3.one * sunSize;
        }
        if (moonSprite)
        {
            moonSprite.position = cam.transform.position + moonDir * skyDistance;
            moonSprite.localScale = Vector3.one * moonSize;
        }

        // wygaszanie/widocznoœæ na podstawie czasu doby
        float t = cycle ? cycle.time01 : 0f;

        // domyœlnie: s³oñce ~ w dzieñ, ksiê¿yc ~ w nocy
        float sunAlpha = sunOpacityOverDay.Evaluate(t);
        float moonAlpha = moonOpacityOverDay.Evaluate(t);

        // Jeœli masz w DayNightCycle blendCurve (0 dzieñ, 1 noc), mo¿esz spi¹æ tak:
        if (cycle && cycle.blendCurve != null && cycle.blendedSkybox != null)
        {
            float night = Mathf.Clamp01(cycle.blendCurve.Evaluate(t));
            // miks: s³oñce = 1-noc, ksiê¿yc = noc (z miêkk¹ ramp¹)
            sunAlpha = Mathf.SmoothStep(0f, 1f, 1f - night);
            moonAlpha = Mathf.SmoothStep(0f, 1f, night);
        }

        // ustaw alpha w materiale (musi wspieraæ alfa)
        if (_sunMat)
        {
            var c = _sunMat.color; c.a = sunAlpha;
            _sunMat.color = c;
        }
        if (_moonMat)
        {
            var c = _moonMat.color; c.a = moonAlpha;
            _moonMat.color = c;
        }
    }
}
