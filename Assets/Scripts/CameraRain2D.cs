using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class CameraRain2D : MonoBehaviour
{
    [Header("Coverage")]
    public bool fitToCameraView = true;
    public float horizontalPadding = 2f;
    public float verticalPadding = 2f;
    public float spawnBandHeight = 1.5f;
    public float width = 22f;
    public float height = 14f;
    public float zOffset = 10f;

    [Header("Drops")]
    public int maxParticles = 400;
    public float emissionRate = 180f;
    public Vector2 lifetimeRange = new Vector2(0.9f, 1.15f);
    public Vector2 fallSpeedRange = new Vector2(11f, 15f);
    public Vector2 sizeRange = new Vector2(0.015f, 0.03f);
    public float sidewaysDrift = -0.35f;
    public Color dropColor = new Color(0.82f, 0.88f, 1f, 0.22f);

    [Header("Rendering")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 20;
    public float streakLength = 0.4f;
    public float streakVelocityScale = 0.08f;

    [Header("Light Reveal")]
    public bool revealOnlyInPlayerLight = true;
    public Transform revealTarget;
    public Light2D revealLight;
    public float revealSoftness = 1.25f;
    public float fallbackRevealRadius = 3f;

    const string RainObjectName = "Rain FX";

    Transform rainRoot;
    ParticleSystem rainSystem;
    ParticleSystemRenderer rainRenderer;
    Material runtimeMaterial;

    void OnEnable()
    {
        if (!Application.isPlaying) return;

        ClampRanges();
        EnsureRainObject();
        ApplySettings();
        rainSystem.Play(true);
    }

    void OnValidate()
    {
        ClampRanges();

        if (!Application.isPlaying || rainSystem == null) return;
        ApplySettings();
    }

    void OnDisable()
    {
        if (!Application.isPlaying) return;

        if (rainSystem != null)
        {
            rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (rainRoot != null)
        {
            Destroy(rainRoot.gameObject);
            rainRoot = null;
            rainSystem = null;
            rainRenderer = null;
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    void ClampRanges()
    {
        horizontalPadding = Mathf.Max(0f, horizontalPadding);
        verticalPadding = Mathf.Max(0f, verticalPadding);
        spawnBandHeight = Mathf.Max(0.1f, spawnBandHeight);
        width = Mathf.Max(1f, width);
        height = Mathf.Max(1f, height);
        zOffset = Mathf.Max(0.1f, zOffset);

        maxParticles = Mathf.Max(100, maxParticles);
        emissionRate = Mathf.Max(0f, emissionRate);

        if (lifetimeRange.x > lifetimeRange.y)
        {
            lifetimeRange = new Vector2(lifetimeRange.y, lifetimeRange.x);
        }
        lifetimeRange.x = Mathf.Max(0.05f, lifetimeRange.x);
        lifetimeRange.y = Mathf.Max(lifetimeRange.x, lifetimeRange.y);

        if (fallSpeedRange.x > fallSpeedRange.y)
        {
            fallSpeedRange = new Vector2(fallSpeedRange.y, fallSpeedRange.x);
        }
        fallSpeedRange.x = Mathf.Max(0.5f, fallSpeedRange.x);
        fallSpeedRange.y = Mathf.Max(fallSpeedRange.x, fallSpeedRange.y);

        if (sizeRange.x > sizeRange.y)
        {
            sizeRange = new Vector2(sizeRange.y, sizeRange.x);
        }
        sizeRange.x = Mathf.Max(0.005f, sizeRange.x);
        sizeRange.y = Mathf.Max(sizeRange.x, sizeRange.y);

        streakLength = Mathf.Max(0.1f, streakLength);
        streakVelocityScale = Mathf.Max(0f, streakVelocityScale);
        revealSoftness = Mathf.Max(0.01f, revealSoftness);
        fallbackRevealRadius = Mathf.Max(0.1f, fallbackRevealRadius);
    }

    void EnsureRainObject()
    {
        Transform existing = transform.Find(RainObjectName);
        if (existing == null)
        {
            GameObject rainObject = new GameObject(RainObjectName);
            rainObject.layer = gameObject.layer;
            existing = rainObject.transform;
            existing.SetParent(transform, false);
        }

        rainRoot = existing;
        rainRoot.localPosition = new Vector3(0f, 0f, zOffset);
        rainRoot.localRotation = Quaternion.identity;
        rainRoot.localScale = Vector3.one;

        rainSystem = rainRoot.GetComponent<ParticleSystem>();
        if (rainSystem == null)
        {
            rainSystem = rainRoot.gameObject.AddComponent<ParticleSystem>();
        }

        rainRenderer = rainRoot.GetComponent<ParticleSystemRenderer>();
        if (rainRenderer == null)
        {
            rainRenderer = rainRoot.gameObject.AddComponent<ParticleSystemRenderer>();
        }
    }

    void ApplySettings()
    {
        Vector2 coverage = GetCoverageSize();
        float spawnY = (coverage.y * 0.5f) + (spawnBandHeight * 0.5f);
        rainRoot.localPosition = new Vector3(0f, 0f, zOffset);

        var main = rainSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = new Color(dropColor.r, dropColor.g, dropColor.b, 1f);
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Local;

        var emission = rainSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = rainSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(coverage.x, spawnBandHeight, 0.1f);
        shape.position = new Vector3(0f, spawnY, 0f);

        var velocityOverLifetime = rainSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(sidewaysDrift, sidewaysDrift);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-fallSpeedRange.y, -fallSpeedRange.x);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOverLifetime = rainSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildFadeGradient());

        rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        rainRenderer.lengthScale = streakLength;
        rainRenderer.velocityScale = streakVelocityScale;
        rainRenderer.cameraVelocityScale = 0f;
        rainRenderer.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        rainRenderer.sortingOrder = sortingOrder;
        rainRenderer.sharedMaterial = GetOrCreateMaterial();
        UpdateRevealMaterial();

        if (rainSystem.isPlaying)
        {
            rainSystem.Clear();
            rainSystem.Play(true);
        }
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || runtimeMaterial == null) return;
        UpdateRevealMaterial();
    }

    Vector2 GetCoverageSize()
    {
        if (fitToCameraView)
        {
            Camera attachedCamera = GetComponent<Camera>();
            if (attachedCamera != null && attachedCamera.orthographic)
            {
                float worldHeight = attachedCamera.orthographicSize * 2f;
                float worldWidth = worldHeight * attachedCamera.aspect;
                return new Vector2(worldWidth + horizontalPadding, worldHeight + verticalPadding);
            }
        }

        return new Vector2(width, height);
    }

    Gradient BuildFadeGradient()
    {
        float alpha = Mathf.Clamp01(dropColor.a);
        Color color = new Color(dropColor.r, dropColor.g, dropColor.b, 1f);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(alpha, 0.1f),
                new GradientAlphaKey(alpha * 0.9f, 0.85f),
                new GradientAlphaKey(0f, 1f)
            });

        return gradient;
    }

    Material GetOrCreateMaterial()
    {
        if (runtimeMaterial != null) return runtimeMaterial;

        Shader shader = Shader.Find("MajorProject/RainReveal");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) return null;

        runtimeMaterial = new Material(shader);
        runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
        return runtimeMaterial;
    }

    void UpdateRevealMaterial()
    {
        if (runtimeMaterial == null || !runtimeMaterial.HasProperty("_RevealEnabled")) return;

        bool useReveal = revealOnlyInPlayerLight && revealTarget != null;
        runtimeMaterial.SetFloat("_RevealEnabled", useReveal ? 1f : 0f);

        if (!useReveal) return;

        float revealRadius = fallbackRevealRadius;
        if (revealLight != null)
        {
            revealRadius = Mathf.Max(revealRadius, revealLight.pointLightOuterRadius);
        }

        Vector3 revealCenter = revealTarget.position;
        runtimeMaterial.SetVector("_RevealCenter", new Vector4(revealCenter.x, revealCenter.y, 0f, 0f));
        runtimeMaterial.SetFloat("_RevealRadius", revealRadius);
        runtimeMaterial.SetFloat("_RevealSoftness", revealSoftness);
    }
}
