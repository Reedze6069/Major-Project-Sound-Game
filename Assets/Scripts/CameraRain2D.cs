using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class CameraRain2D : MonoBehaviour
{
    [Header("Coverage")]
    // When true, the rain area matches the attached orthographic camera view.
    public bool fitToCameraView = true;
    // Extra width added to the camera-sized rain area.
    public float horizontalPadding = 2f;
    // Extra height added to the camera-sized rain area.
    public float verticalPadding = 2f;
    // Height of the invisible spawn band above the camera.
    public float spawnBandHeight = 1.5f;
    // Manual rain width used when fitToCameraView is false.
    public float width = 22f;
    // Manual rain height used when fitToCameraView is false.
    public float height = 14f;
    // Moves rain particles forward from the camera so they render in view.
    public float zOffset = 10f;

    [Header("Drops")]
    // Maximum number of rain particles alive at once.
    public int maxParticles = 400;
    // Number of drops emitted per second.
    public float emissionRate = 180f;
    // Random lifetime range for each particle.
    public Vector2 lifetimeRange = new Vector2(0.9f, 1.15f);
    // Random downward speed range for each particle.
    public Vector2 fallSpeedRange = new Vector2(11f, 15f);
    // Random size range for each rain drop.
    public Vector2 sizeRange = new Vector2(0.015f, 0.03f);
    // Horizontal wind-like movement applied while drops fall.
    public float sidewaysDrift = -0.35f;
    // Base drop color; alpha controls how visible the rain is.
    public Color dropColor = new Color(0.82f, 0.88f, 1f, 0.22f);

    [Header("Rendering")]
    // Sorting layer used by the particle renderer.
    public string sortingLayerName = "Default";
    // Sorting order used to place rain in front of or behind sprites.
    public int sortingOrder = 20;
    // Stretch length for each falling rain streak.
    public float streakLength = 0.4f;
    // How much particle velocity affects streak stretching.
    public float streakVelocityScale = 0.08f;

    [Header("Light Reveal")]
    // When true, the shader hides rain outside the player's light radius.
    public bool revealOnlyInPlayerLight = true;
    // Transform used as the center of the reveal area.
    public Transform revealTarget;
    // Optional Light2D used to size the reveal radius automatically.
    public Light2D revealLight;
    // Soft edge size for the reveal shader.
    public float revealSoftness = 1.25f;
    // Radius used when no Light2D is assigned or the light radius is smaller.
    public float fallbackRevealRadius = 3f;

    // Name of the generated child object that owns the particle system.
    const string RainObjectName = "Rain FX";

    // Cached runtime objects created and managed by this component.
    Transform rainRoot;
    ParticleSystem rainSystem;
    ParticleSystemRenderer rainRenderer;
    Material runtimeMaterial;

    void OnEnable()
    {
        // Only create runtime particle objects while the game is playing.
        if (!Application.isPlaying) return;

        // Validate inspector values, build the particle object, then start rain.
        ClampRanges();
        EnsureRainObject();
        ApplySettings();
        rainSystem.Play(true);
    }

    void OnValidate()
    {
        // Clamp values immediately when edited in the Inspector.
        ClampRanges();

        // In play mode, reapply particle settings after inspector changes.
        if (!Application.isPlaying || rainSystem == null) return;
        ApplySettings();
    }

    void OnDisable()
    {
        // Do not destroy generated objects while Unity is only editing the scene.
        if (!Application.isPlaying) return;

        // Stop and clear the particle system before destroying its child object.
        if (rainSystem != null)
        {
            rainSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Remove the generated rain object so it does not linger after disabling.
        if (rainRoot != null)
        {
            Destroy(rainRoot.gameObject);
            rainRoot = null;
            rainSystem = null;
            rainRenderer = null;
        }

        // Destroy the runtime material because it was created by this script.
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    void ClampRanges()
    {
        // Prevent negative or zero values that would break particle coverage.
        horizontalPadding = Mathf.Max(0f, horizontalPadding);
        verticalPadding = Mathf.Max(0f, verticalPadding);
        spawnBandHeight = Mathf.Max(0.1f, spawnBandHeight);
        width = Mathf.Max(1f, width);
        height = Mathf.Max(1f, height);
        zOffset = Mathf.Max(0.1f, zOffset);

        maxParticles = Mathf.Max(100, maxParticles);
        emissionRate = Mathf.Max(0f, emissionRate);

        // Ensure lifetime min and max are in the correct order.
        if (lifetimeRange.x > lifetimeRange.y)
        {
            lifetimeRange = new Vector2(lifetimeRange.y, lifetimeRange.x);
        }
        lifetimeRange.x = Mathf.Max(0.05f, lifetimeRange.x);
        lifetimeRange.y = Mathf.Max(lifetimeRange.x, lifetimeRange.y);

        // Ensure fall speed min and max are in the correct order.
        if (fallSpeedRange.x > fallSpeedRange.y)
        {
            fallSpeedRange = new Vector2(fallSpeedRange.y, fallSpeedRange.x);
        }
        fallSpeedRange.x = Mathf.Max(0.5f, fallSpeedRange.x);
        fallSpeedRange.y = Mathf.Max(fallSpeedRange.x, fallSpeedRange.y);

        // Ensure size min and max are in the correct order.
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
        // Reuse an existing child if it was already created.
        Transform existing = transform.Find(RainObjectName);
        if (existing == null)
        {
            // Create a child object to keep rain setup separate from the camera.
            GameObject rainObject = new GameObject(RainObjectName);
            rainObject.layer = gameObject.layer;
            existing = rainObject.transform;
            existing.SetParent(transform, false);
        }

        rainRoot = existing;
        // Reset local transform so all particle settings are predictable.
        rainRoot.localPosition = new Vector3(0f, 0f, zOffset);
        rainRoot.localRotation = Quaternion.identity;
        rainRoot.localScale = Vector3.one;

        // Add or cache the particle system that will emit the rain drops.
        rainSystem = rainRoot.GetComponent<ParticleSystem>();
        if (rainSystem == null)
        {
            rainSystem = rainRoot.gameObject.AddComponent<ParticleSystem>();
        }

        // Add or cache the renderer so rain can be sorted and stretched.
        rainRenderer = rainRoot.GetComponent<ParticleSystemRenderer>();
        if (rainRenderer == null)
        {
            rainRenderer = rainRoot.gameObject.AddComponent<ParticleSystemRenderer>();
        }
    }

    void ApplySettings()
    {
        // Calculate how wide and tall the rain area should be.
        Vector2 coverage = GetCoverageSize();
        // Spawn particles above the visible area so they fall into frame.
        float spawnY = (coverage.y * 0.5f) + (spawnBandHeight * 0.5f);
        rainRoot.localPosition = new Vector3(0f, 0f, zOffset);

        // Main module controls the lifetime, size, color, and simulation space.
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

        // Emission module controls how many drops are created over time.
        var emission = rainSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        // Shape module creates drops across a horizontal band above the camera.
        var shape = rainSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(coverage.x, spawnBandHeight, 0.1f);
        shape.position = new Vector3(0f, spawnY, 0f);

        // Velocity module makes drops fall downward with optional sideways drift.
        var velocityOverLifetime = rainSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(sidewaysDrift, sidewaysDrift);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-fallSpeedRange.y, -fallSpeedRange.x);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Color module fades drops in and out during their lifetime.
        var colorOverLifetime = rainSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildFadeGradient());

        // Renderer settings make each particle look like a stretched rain streak.
        rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        rainRenderer.lengthScale = streakLength;
        rainRenderer.velocityScale = streakVelocityScale;
        rainRenderer.cameraVelocityScale = 0f;
        rainRenderer.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        rainRenderer.sortingOrder = sortingOrder;
        rainRenderer.sharedMaterial = GetOrCreateMaterial();
        UpdateRevealMaterial();

        // Restart active rain so changed settings take effect immediately.
        if (rainSystem.isPlaying)
        {
            rainSystem.Clear();
            rainSystem.Play(true);
        }
    }

    void LateUpdate()
    {
        // Keep shader reveal data synced with the moving player and light.
        if (!Application.isPlaying || runtimeMaterial == null) return;
        UpdateRevealMaterial();
    }

    Vector2 GetCoverageSize()
    {
        // Prefer the camera view size when this component is on an orthographic camera.
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

        // Fall back to manual dimensions if there is no usable camera.
        return new Vector2(width, height);
    }

    Gradient BuildFadeGradient()
    {
        // Store alpha separately so the particle color itself stays fully tinted.
        float alpha = Mathf.Clamp01(dropColor.a);
        Color color = new Color(dropColor.r, dropColor.g, dropColor.b, 1f);

        // Fade in at spawn, stay visible briefly, then fade out before death.
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
        // Reuse the material once it has been created.
        if (runtimeMaterial != null) return runtimeMaterial;

        // Prefer the custom reveal shader, then fall back to built-in particle shaders.
        Shader shader = Shader.Find("MajorProject/RainReveal");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) return null;

        // HideAndDontSave keeps the runtime material out of the saved scene.
        runtimeMaterial = new Material(shader);
        runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
        return runtimeMaterial;
    }

    void UpdateRevealMaterial()
    {
        // Skip reveal updates if the fallback material does not support them.
        if (runtimeMaterial == null || !runtimeMaterial.HasProperty("_RevealEnabled")) return;

        // Reveal only works when enabled and a target has been assigned.
        bool useReveal = revealOnlyInPlayerLight && revealTarget != null;
        runtimeMaterial.SetFloat("_RevealEnabled", useReveal ? 1f : 0f);

        if (!useReveal) return;

        // Use the light radius when available, but never smaller than the fallback.
        float revealRadius = fallbackRevealRadius;
        if (revealLight != null)
        {
            revealRadius = Mathf.Max(revealRadius, revealLight.pointLightOuterRadius);
        }

        // Send the reveal circle data to the shader.
        Vector3 revealCenter = revealTarget.position;
        runtimeMaterial.SetVector("_RevealCenter", new Vector4(revealCenter.x, revealCenter.y, 0f, 0f));
        runtimeMaterial.SetFloat("_RevealRadius", revealRadius);
        runtimeMaterial.SetFloat("_RevealSoftness", revealSoftness);
    }
}
