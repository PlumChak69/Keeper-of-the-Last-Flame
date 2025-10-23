using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DayNightCycle : MonoBehaviour
{
    [Header("Directional Light (Sun)")]
    public Light directionalLight;
    [Tooltip("Czas trwania pe³nego cyklu dnia w sekundach")]
    public float dayDuration = 120f;
    [Range(0f, 1f)] public float time01 = 0f; // 0 = œwit, 0.5 = zmierzch, 1 = noc

    [Header("Intensity / Colors")]
    public AnimationCurve lightIntensityCurve;
    public Gradient lightColorGradient;

    [Header("Fog (optional)")]
    public bool useFog = true;
    public Gradient fogColorGradient;
    public AnimationCurve fogDensityCurve;

    [Header("Skybox Blend (Cubemaps)")]
    public Material blendedSkybox;           // przypisz materia³ Skybox_BlendCubemap
    public AnimationCurve exposureCurve;     // 0..1 -> Exposure
    public AnimationCurve blendCurve;        // 0..1 -> Blend (0 dzieñ, 1 noc)
    public bool updateGI = true;

    [Header("Skybox Rotation")]
    public bool rotateSkybox = true;
    [Tooltip("Stopnie/sekunda - rotacja cubemapy dnia")]
    public float dayCubeYawSpeed = 1f;
    [Tooltip("Stopnie/sekunda - rotacja cubemapy nocy")]
    public float nightCubeYawSpeed = 0.5f;
    [Tooltip("Jeœli true – noc obraca siê tak samo jak dzieñ (z przesuniêciem).")]
    public bool lockNightToDay = false;
    [Tooltip("Przesuniêcie k¹ta nocnej cubemapy wzglêdem dnia (gdy lockNightToDay = true)")]
    public float nightOffsetDeg = 0f;

    private float _cycleTimer = 0f;
    private float _dayYaw = 0f;
    private float _nightYaw = 0f;

    //====================================================
    void Reset()
    {
        // Ustaw œwiat³o s³oneczne, jeœli brak
        if (directionalLight == null)
        {
            Light dirLight = RenderSettings.sun;
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.color = Color.white;
            }
            directionalLight = dirLight;
        }

        // Domyœlne krzywe i gradienty
        lightIntensityCurve = new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(0.15f, 0.6f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(0.75f, 0.2f),
            new Keyframe(1f, 0.1f)
        );

        lightColorGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0f),  // œwit
                new GradientColorKey(Color.white, 0.25f),             // dzieñ
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.5f),// zmierzch
                new GradientColorKey(new Color(0.2f, 0.3f, 0.5f), 1f) // noc
            }
        };

        fogColorGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.5f,0.5f,0.6f),0f),
                new GradientColorKey(new Color(0.7f,0.8f,1f),0.3f),
                new GradientColorKey(new Color(0.3f,0.3f,0.4f),1f)
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
    }

    //====================================================
    void Update()
    {
        if (dayDuration <= 0) return;

        // Aktualizacja czasu cyklu
        _cycleTimer += Application.isPlaying ? Time.deltaTime : 0f;
        time01 = Mathf.Repeat(_cycleTimer / dayDuration, 1f);

        // --- Obrót s³oñca ---
        if (directionalLight)
        {
            float rotation = time01 * 360f - 90f; // -90 = œwit
            directionalLight.transform.rotation = Quaternion.Euler(rotation, 170f, 0f);

            // Ustaw intensywnoœæ i kolor
            directionalLight.intensity = lightIntensityCurve.Evaluate(time01);
            directionalLight.color = lightColorGradient.Evaluate(time01);
        }

        // --- Mg³a ---
        if (useFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColorGradient.Evaluate(time01);
            RenderSettings.fogDensity = fogDensityCurve.Evaluate(time01);
        }

        // --- Skybox Exposure & Blend ---
        if (blendedSkybox != null)
        {
            float exposure = Mathf.Clamp(exposureCurve.Evaluate(time01), 0.05f, 3f);
            float blend = Mathf.Clamp01(blendCurve.Evaluate(time01));

            if (blendedSkybox.HasProperty("_Exposure"))
                blendedSkybox.SetFloat("_Exposure", exposure);

            if (blendedSkybox.HasProperty("_Blend"))
                blendedSkybox.SetFloat("_Blend", blend);

            // --- Rotacja cubemap ---
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

            // Aktualizacja oœwietlenia globalnego
            if (updateGI && Application.isPlaying)
                DynamicGI.UpdateEnvironment();
        }
    }

    //====================================================
    [ContextMenu("Apply Skybox Curves Preset")]
    public void ApplySkyboxCurvesPreset()
    {
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

        blendCurve = new AnimationCurve(
            new Keyframe(0.00f, 1.00f), // noc
            new Keyframe(0.15f, 0.25f), // œwit
            new Keyframe(0.25f, 0.00f), // dzieñ
            new Keyframe(0.55f, 0.25f), // zmierzch
            new Keyframe(0.75f, 1.00f), // noc
            new Keyframe(1.00f, 1.00f)
        );
        for (int i = 0; i < blendCurve.keys.Length; i++)
            blendCurve.SmoothTangents(i, 0.25f);
    }
}