using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DayNightCycle : MonoBehaviour
{
    [Header("Directional Light (Sun)")]
    public Light directionalLight;
    [Tooltip("Czas trwania pełnego cyklu doby (s).")]
    public float dayDuration = 120f;
    [Range(0f, 1f)] public float time01 = 0f; // 0..1 postęp doby (świt→dzień→zmierzch→noc)

    [Header("Intensity / Color")]
    public AnimationCurve lightIntensityCurve;
    public Gradient lightColorGradient;

    [Header("Fog (optional)")]
    public bool useFog = true;
    public Gradient fogColorGradient;
    public AnimationCurve fogDensityCurve;

    [Header("Skybox Blend (Cubemaps shader)")]
    public Material blendedSkybox;        // materiał z Shaderem "Skybox/BlendCubemaps"
    public AnimationCurve exposureCurve;  // 0..1 -> _Exposure
    public AnimationCurve blendCurve;     // 0..1 -> _Blend (0 dzień, 1 noc)
    public bool updateGI = true;          // DynamicGI.UpdateEnvironment()

    [Header("Skybox Rotation")]
    public bool rotateSkybox = true;
    [Tooltip("deg/sec — prędkość obrotu cubemapy dnia")]
    public float dayCubeYawSpeed = 1f;
    [Tooltip("deg/sec — prędkość obrotu cubemapy nocy")]
    public float nightCubeYawSpeed = 0.5f;
    [Tooltip("Jeśli true — noc obraca się jak dzień z offsetem.")]
    public bool lockNightToDay = false;
    public float nightOffsetDeg = 0f;

    [Header("Gameplay Hooks")]
    public FlameEnergy flame;                     // przypnij obiekt z FlameEnergy
    [Tooltip("Próg intensywności, poniżej którego uznajemy noc (fallback).")]
    public float nightIntensityThreshold = 0.2f;
    [Tooltip("Histereza progowa, by nie klikało przy świcie/zmierzchu.")]
    public float nightHysteresis = 0.05f;         // np. 0.05 = 5% marginesu

    // Internal state
    private float _cycleTimer = 0f;
    private float _dayYaw = 0f, _nightYaw = 0f;
    private bool _isNightCached = false;          // do histerezy

    // ================================================================
    void Reset()
    {
        // światło słoneczne
        if (!directionalLight)
        {
            var sun = RenderSettings.sun;
            if (!sun)
            {
                var go = new GameObject("Directional Light");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1f;
                sun.color = Color.white;
            }
            directionalLight = sun;
        }

        if (!flame) flame = FindFirstObjectByType<FlameEnergy>();

        // Domyślne krzywe
        lightIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(0.15f, 0.6f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(0.75f, 0.2f),
            new Keyframe(1f, 0.1f)
        );

        lightColorGradient = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(new Color(1f,0.6f,0.4f), 0f),   // świt
                new GradientColorKey(Color.white,               0.25f),// dzień
                new GradientColorKey(new Color(1f,0.5f,0.2f),  0.5f), // zmierzch
                new GradientColorKey(new Color(0.2f,0.3f,0.5f),1f)    // noc
            }
        };

        fogColorGradient = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(new Color(0.5f,0.5f,0.6f), 0f),
                new GradientColorKey(new Color(0.7f,0.8f,1f),   0.3f),
                new GradientColorKey(new Color(0.3f,0.3f,0.4f), 1f)
            }
        };

        fogDensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.01f),
            new Keyframe(0.25f, 0.002f),
            new Keyframe(0.5f, 0.004f),
            new Keyframe(0.75f, 0.01f),
            new Keyframe(1f, 0.015f)
        );

        ApplySkyboxCurvesPreset();

        _dayYaw = 0f;
        _nightYaw = nightOffsetDeg;
        _isNightCached = false;
    }

    // ================================================================
    void Update()
    {
        if (dayDuration <= 0f) return;

        // Czas doby 0..1
        _cycleTimer += Application.isPlaying ? Time.deltaTime : 0f;
        time01 = Mathf.Repeat(_cycleTimer / dayDuration, 1f);

        // Obrót i parametry światła
        if (directionalLight)
        {
            float rotX = time01 * 360f - 90f; // -90 = świt
            directionalLight.transform.rotation = Quaternion.Euler(rotX, 170f, 0f);
            directionalLight.intensity = lightIntensityCurve.Evaluate(time01);
            directionalLight.color = lightColorGradient.Evaluate(time01);
        }

        // Mgła
        if (useFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColorGradient.Evaluate(time01);
            RenderSettings.fogDensity = fogDensityCurve.Evaluate(time01);
        }

        // Skybox: Exposure + Blend + Rotacja
        float blend01 = 0f;
        if (blendedSkybox)
        {
            float exposure = Mathf.Clamp(exposureCurve.Evaluate(time01), 0.05f, 3f);
            blend01 = Mathf.Clamp01(blendCurve.Evaluate(time01));

            if (blendedSkybox.HasProperty("_Exposure"))
                blendedSkybox.SetFloat("_Exposure", exposure);
            if (blendedSkybox.HasProperty("_Blend"))
                blendedSkybox.SetFloat("_Blend", blend01);

            if (rotateSkybox && Application.isPlaying)
            {
                _dayYaw = (_dayYaw + dayCubeYawSpeed * Time.deltaTime) % 360f;
                if (lockNightToDay)
                    _nightYaw = (_dayYaw + nightOffsetDeg) % 360f;
                else
                    _nightYaw = (_nightYaw + nightCubeYawSpeed * Time.deltaTime) % 360f;
            }

            if (blendedSkybox.HasProperty("_RotDayDeg"))
                blendedSkybox.SetFloat("_RotDayDeg", _dayYaw);
            if (blendedSkybox.HasProperty("_RotNightDeg"))
                blendedSkybox.SetFloat("_RotNightDeg", _nightYaw);

            if (updateGI && Application.isPlaying)
                DynamicGI.UpdateEnvironment();
        }

        // Wylicz NOC/DZIEŃ i ustaw FlameEnergy.isNight (z histerezą)
        if (flame != null)
        {
            bool targetNight;

            // Jeśli mamy blendCurve (0 dzień → 1 noc), używaj jej
            if (blendedSkybox && blendCurve != null && blendCurve.length > 0)
            {
                // próg z histerezą: przełącz dopiero, gdy przekroczy 0.5±h
                float thresholdUp = 0.5f + nightHysteresis;   // dzień->noc
                float thresholdDn = 0.5f - nightHysteresis;   // noc->dzień

                if (!_isNightCached)
                    targetNight = (blend01 >= thresholdUp);
                else
                    targetNight = (blend01 >= thresholdDn);
            }
            else
            {
                // Fallback: po intensywności światła z histerezą
                float intensity = directionalLight ? directionalLight.intensity : 0f;
                float up = nightIntensityThreshold + nightHysteresis;
                float dn = nightIntensityThreshold - nightHysteresis;

                if (!_isNightCached)
                    targetNight = (intensity < dn);
                else
                    targetNight = (intensity < up);
            }

            _isNightCached = targetNight;
            flame.isNight = _isNightCached;
        }
    }

    // ================================================================
    [ContextMenu("Apply Skybox Curves Preset")]
    public void ApplySkyboxCurvesPreset()
    {
        // Exposure: ciemny świt → jasny dzień → ciemny zmierzch → bardzo ciemna noc
        exposureCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.15f),
            new Keyframe(0.15f, 0.60f),
            new Keyframe(0.25f, 1.00f),
            new Keyframe(0.50f, 0.50f),
            new Keyframe(0.75f, 0.20f),
            new Keyframe(1.00f, 0.15f)
        );
        for (int i = 0; i < exposureCurve.keys.Length; i++)
            exposureCurve.SmoothTangents(i, 0.25f);

        // Blend: 0 dzień → 1 noc (noc: północ i okolice 0/1; dzień: ok. 0.25–0.5)
        blendCurve = new AnimationCurve(
            new Keyframe(0.00f, 1.00f), // noc
            new Keyframe(0.15f, 0.25f), // świt
            new Keyframe(0.25f, 0.00f), // dzień
            new Keyframe(0.55f, 0.25f), // zmierzch
            new Keyframe(0.75f, 1.00f), // noc
            new Keyframe(1.00f, 1.00f)  // noc
        );
        for (int i = 0; i < blendCurve.keys.Length; i++)
            blendCurve.SmoothTangents(i, 0.25f);
    }
}