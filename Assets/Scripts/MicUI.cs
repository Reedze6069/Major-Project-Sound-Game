using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MicUI : MonoBehaviour
{
    // Microphone source that provides the live amplitude value.
    public MicrophoneInput mic;
    // Voice controller that provides the current action state and thresholds.
    public VoiceActionController voice;

    [Header("Meter")]
    // Slider used as the visual microphone meter.
    public Slider meter;
    // Fill image tinted according to the current voice state.
    public Image meterFill;
    // Optional standalone text label for the current action.
    public TMP_Text stateText;

    // Maximum slider value when no VoiceActionController is assigned.
    public float meterMax = 0.5f;
    // SmoothDamp time used to soften meter movement.
    [Range(0.01f, 0.5f)] public float meterSmoothTime = 0.12f;

    [Header("Compact Layout")]
    // Height forced onto the meter RectTransform.
    [Min(12f)] public float meterHeight = 24f;
    // Width of the slider handle indicator.
    [Min(1f)] public float handleIndicatorWidth = 6f;
    // Transparency of the slider handle indicator.
    [Range(0f, 1f)] public float handleIndicatorAlpha = 1f;
    // Width of threshold divider lines.
    [Min(0.5f)] public float dividerWidth = 1f;
    // Vertical padding applied to threshold divider lines.
    [Min(0f)] public float dividerVerticalInset = 4f;

    [Header("Zone Styling")]
    // Hides the separate action text when labels are drawn inside the meter.
    public bool hideStandaloneStateText = true;
    // Background opacity for inactive zones.
    [Range(0f, 1f)] public float inactiveZoneAlpha = 0.1f;
    // Background opacity for the active zone.
    [Range(0f, 1f)] public float activeZoneAlpha = 0.42f;
    // Label opacity for inactive zones.
    [Range(0f, 1f)] public float inactiveLabelAlpha = 0.55f;
    // Label opacity for the active zone.
    [Range(0f, 1f)] public float activeLabelAlpha = 0.95f;
    // Color for the idle meter fill.
    public Color idleColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    // Color for the quiet/crouch zone.
    public Color crouchColor = new Color(0.24f, 0.86f, 0.34f, 1f);
    // Color for the medium/light zone.
    public Color lightColor = new Color(1f, 0.42f, 0.05f, 1f);
    // Color for the loud/jump zone.
    public Color jumpColor = new Color(1f, 0.32f, 0.32f, 1f);
    // Color used by threshold divider lines.
    public Color dividerColor = new Color(1f, 1f, 1f, 0.12f);

    // Current displayed meter value after smoothing.
    float displayedMeterValue;
    // SmoothDamp velocity used for the meter value.
    float meterVelocity;

    // Generated layer that holds colored meter backgrounds.
    RectTransform zoneBackdrop;
    // Generated layer that holds labels and threshold dividers.
    RectTransform zoneLabelOverlay;
    // One background image for each voice action zone.
    readonly Image[] zoneBackgrounds = new Image[3];
    // One text label for each voice action zone.
    readonly TMP_Text[] zoneLabels = new TMP_Text[3];
    // Divider images that mark the quiet/medium and medium/loud thresholds.
    readonly Image[] zoneDividers = new Image[2];
    // White marker drawn on top of the meter so the current input level is always visible.
    Image meterIndicator;

    void Awake()
    {
        // Build the meter zone UI as soon as the component starts.
        EnsureMeterZones();
    }

    void Update()
    {
        // Re-check generated UI in case Unity references changed in the Inspector.
        EnsureMeterZones();

        if (meter != null)
        {
            // Keep the slider range synced to the voice threshold setup.
            meter.minValue = 0f;
            meter.maxValue = ResolveMeterMax();

            // Smooth the displayed amplitude so the meter is readable.
            float smoothedAmplitude = mic != null ? mic.SmoothedAmplitude : 0f;
            float targetValue = Mathf.Min(smoothedAmplitude, meter.maxValue);
            displayedMeterValue = Mathf.SmoothDamp(displayedMeterValue, targetValue, ref meterVelocity, meterSmoothTime);
            meter.value = displayedMeterValue;
        }

        UpdateMeterColors();
        UpdateZoneLayout();
        UpdateZoneColors();
        UpdateMeterIndicator();
        UpdateStateText();
    }

    void EnsureMeterZones()
    {
        // The generated zone UI needs a slider to attach to.
        if (meter == null)
            return;

        // Sliders should be RectTransforms inside Unity UI.
        RectTransform meterRect = meter.transform as RectTransform;
        if (meterRect == null)
            return;

        // Apply the compact visual setup before laying out child zones.
        ApplyCompactLayout(meterRect);

        // Find or create the two generated layers inside the meter.
        zoneBackdrop = FindOrCreateLayer(meterRect, "MeterZones", 0);
        zoneLabelOverlay = FindOrCreateLayer(meterRect, "MeterLabels", meterRect.childCount);

        CreateZoneBackground(0, "CrouchZone", zoneBackdrop);
        CreateZoneBackground(1, "LightZone", zoneBackdrop);
        CreateZoneBackground(2, "JumpZone", zoneBackdrop);

        CreateZoneLabel(0, "CrouchLabel", "Crouch", zoneLabelOverlay);
        CreateZoneLabel(1, "LightLabel", "Light", zoneLabelOverlay);
        CreateZoneLabel(2, "JumpLabel", "Jump", zoneLabelOverlay);

        CreateDivider(0, "DividerQuietMedium", zoneLabelOverlay);
        CreateDivider(1, "DividerMediumLoud", zoneLabelOverlay);

        EnsureMeterIndicator(meterRect);
    }

    void UpdateMeterColors()
    {
        // Without a fill image, there is nothing to tint.
        if (meterFill == null)
            return;

        // Use Idle as the fallback state when no voice controller is assigned.
        VoiceActionController.VoiceState currentState = voice != null
            ? voice.CurrentState
            : VoiceActionController.VoiceState.Idle;

        // Slightly tint the white fill toward the active state color.
        Color tintedFill = Color.Lerp(Color.white, GetStateColor(currentState), 0.25f);
        meterFill.color = WithAlpha(tintedFill, currentState == VoiceActionController.VoiceState.Idle ? 0.14f : 0.28f);
    }

    void UpdateZoneLayout()
    {
        // Wait until zone images and labels have been created.
        if (zoneBackgrounds[0] == null || zoneLabels[0] == null)
            return;

        // Convert voice thresholds into normalized slider positions.
        float resolvedMeterMax = ResolveMeterMax();
        float quietEnd = Mathf.Clamp01(GetQuietMax() / resolvedMeterMax);
        float loudStart = Mathf.Clamp(ResolveLoudMin() / resolvedMeterMax, quietEnd, 1f);

        // Stretch each background and label to cover its threshold range.
        SetStretch(zoneBackgrounds[0].rectTransform, 0f, quietEnd);
        SetStretch(zoneBackgrounds[1].rectTransform, quietEnd, loudStart);
        SetStretch(zoneBackgrounds[2].rectTransform, loudStart, 1f);

        SetStretch(zoneLabels[0].rectTransform, 0f, quietEnd);
        SetStretch(zoneLabels[1].rectTransform, quietEnd, loudStart);
        SetStretch(zoneLabels[2].rectTransform, loudStart, 1f);

        SetDivider(zoneDividers[0], quietEnd);
        SetDivider(zoneDividers[1], loudStart);
    }

    void UpdateZoneColors()
    {
        // These colors match the three action zones: crouch, light, jump.
        Color[] colors =
        {
            crouchColor,
            lightColor,
            jumpColor
        };

        int activeIndex = GetActiveZoneIndex();

        // Tint zone backgrounds and labels, highlighting the active voice zone.
        for (int i = 0; i < zoneBackgrounds.Length; i++)
        {
            if (zoneBackgrounds[i] != null)
                zoneBackgrounds[i].color = WithAlpha(colors[i], i == activeIndex ? activeZoneAlpha : inactiveZoneAlpha);

            if (zoneLabels[i] != null)
            {
                Color labelColor = Color.Lerp(Color.white, colors[i], 0.45f);
                zoneLabels[i].color = WithAlpha(labelColor, i == activeIndex ? activeLabelAlpha : inactiveLabelAlpha);
                zoneLabels[i].fontStyle = FontStyles.Normal;
            }
        }

        // Keep threshold dividers on the configured divider color.
        for (int i = 0; i < zoneDividers.Length; i++)
        {
            if (zoneDividers[i] != null)
                zoneDividers[i].color = dividerColor;
        }
    }

    void UpdateStateText()
    {
        // The standalone label is optional because the meter can label zones itself.
        if (stateText == null)
            return;

        stateText.gameObject.SetActive(!hideStandaloneStateText);
        if (!hideStandaloneStateText)
            stateText.text = $"Action: {GetActionLabel(voice != null ? voice.CurrentState : VoiceActionController.VoiceState.Idle)}";
    }

    void ApplyCompactLayout(RectTransform meterRect)
    {
        // Force a readable meter height even if the Inspector value is too small.
        float resolvedHeight = Mathf.Max(12f, meterHeight);
        meterRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, resolvedHeight);

        // Turn the slider into a display-only meter.
        meter.interactable = false;
        meter.transition = Selectable.Transition.None;

        // Prevent the meter graphics from blocking UI pointer events.
        SetRaycastTarget(meterFill, false);
        SetRaycastTarget(meter.targetGraphic, false);

        // Restyle the default slider background if it exists.
        Transform background = meter.transform.Find("Background");
        Image backgroundImage = background != null ? background.GetComponent<Image>() : null;
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0f, 0f, 0f, 0.18f);
            backgroundImage.raycastTarget = false;
        }

        if (meter.handleRect == null)
            return;

        // Resize the handle so it becomes a thin amplitude indicator.
        RectTransform handleRect = meter.handleRect;
        handleRect.anchorMin = new Vector2(handleRect.anchorMin.x, 0f);
        handleRect.anchorMax = new Vector2(handleRect.anchorMax.x, 1f);
        handleRect.sizeDelta = new Vector2(Mathf.Max(6f, handleIndicatorWidth), 0f);

        if (handleRect.parent != null)
            handleRect.parent.SetAsLastSibling();

        Image handleImage = handleRect.GetComponent<Image>();
        if (handleImage != null)
        {
            // Keep the original slider handle clearly visible.
            handleImage.color = WithAlpha(Color.white, handleIndicatorAlpha);
            handleImage.enabled = true;
            handleImage.raycastTarget = false;
        }
    }

    void EnsureMeterIndicator(RectTransform meterRect)
    {
        if (meterIndicator != null)
            return;

        Transform existing = meterRect.Find("MeterIndicator");
        if (existing != null)
        {
            meterIndicator = existing.GetComponent<Image>();
            if (meterIndicator == null)
                meterIndicator = existing.gameObject.AddComponent<Image>();
        }
        else
        {
            GameObject go = new GameObject("MeterIndicator", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(meterRect, false);
            meterIndicator = go.GetComponent<Image>();
        }

        Image handleImage = meter.handleRect != null ? meter.handleRect.GetComponent<Image>() : null;
        if (handleImage != null)
        {
            meterIndicator.sprite = handleImage.sprite;
            meterIndicator.type = handleImage.type;
        }

        meterIndicator.enabled = true;
        meterIndicator.gameObject.SetActive(true);
        meterIndicator.raycastTarget = false;
        meterIndicator.color = Color.white;
        UpdateMeterIndicator();
    }

    void UpdateMeterIndicator()
    {
        if (meter == null || meterIndicator == null)
            return;

        RectTransform rect = meterIndicator.rectTransform;
        float range = Mathf.Max(0.0001f, meter.maxValue - meter.minValue);
        float normalizedValue = Mathf.Clamp01((meter.value - meter.minValue) / range);

        rect.anchorMin = new Vector2(normalizedValue, 0f);
        rect.anchorMax = new Vector2(normalizedValue, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(Mathf.Max(6f, handleIndicatorWidth), 0f);
        rect.SetAsLastSibling();
    }

    RectTransform FindOrCreateLayer(RectTransform parent, string name, int siblingIndex)
    {
        // Reuse an existing generated layer if one already exists.
        Transform existing = parent.Find(name);
        RectTransform layer;

        if (existing != null)
        {
            layer = existing as RectTransform;
        }
        else
        {
            // Create a RectTransform-only child for generated meter elements.
            GameObject go = new GameObject(name, typeof(RectTransform));
            layer = go.GetComponent<RectTransform>();
            layer.SetParent(parent, false);
        }

        // Stretch the layer over the full meter and place it in the right order.
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = Vector2.zero;
        layer.offsetMax = Vector2.zero;
        layer.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
        return layer;
    }

    void CreateZoneBackground(int index, string name, Transform parent)
    {
        // Do not recreate images that have already been cached.
        if (zoneBackgrounds[index] != null)
            return;

        // Reuse a child with the same name when the UI was generated earlier.
        Transform existing = parent.Find(name);
        Image image;

        if (existing != null)
        {
            image = existing.GetComponent<Image>();
        }
        else
        {
            // Create a colored Image that will stretch across one meter zone.
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>();
        }

        // Background zones should never intercept pointer input.
        image.raycastTarget = false;
        zoneBackgrounds[index] = image;
    }

    void CreateZoneLabel(int index, string name, string labelText, Transform parent)
    {
        // Do not recreate labels that have already been cached.
        if (zoneLabels[index] != null)
            return;

        // Reuse a child with the same name when the UI was generated earlier.
        Transform existing = parent.Find(name);
        TextMeshProUGUI label;

        if (existing != null)
        {
            label = existing.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            // Create a TextMeshPro label for this action zone.
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            label = go.GetComponent<TextMeshProUGUI>();
        }

        // Configure the label to stay compact inside its meter zone.
        label.text = labelText;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 9f;
        label.fontSizeMax = Mathf.Clamp(meterHeight - 8f, 10f, 16f);
        label.margin = new Vector4(4f, 0f, 4f, 0f);
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        zoneLabels[index] = label;
    }

    void CreateDivider(int index, string name, Transform parent)
    {
        // Do not recreate dividers that have already been cached.
        if (zoneDividers[index] != null)
            return;

        // Reuse a child with the same name when the UI was generated earlier.
        Transform existing = parent.Find(name);
        Image divider;

        if (existing != null)
        {
            divider = existing.GetComponent<Image>();
        }
        else
        {
            // Create a thin Image that will mark a threshold boundary.
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            divider = go.GetComponent<Image>();
        }

        // Dividers are visual only.
        divider.raycastTarget = false;
        zoneDividers[index] = divider;
    }

    void SetStretch(RectTransform rect, float start, float end)
    {
        // Skip missing generated elements.
        if (rect == null)
            return;

        // Anchor the element between two normalized horizontal positions.
        rect.anchorMin = new Vector2(start, 0f);
        rect.anchorMax = new Vector2(end, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void SetDivider(Image divider, float position)
    {
        // Skip missing generated dividers.
        if (divider == null)
            return;

        // Anchor the divider to a single normalized X position.
        RectTransform rect = divider.rectTransform;
        rect.anchorMin = new Vector2(position, 0f);
        rect.anchorMax = new Vector2(position, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Keep the divider inset within the meter height.
        float inset = Mathf.Min(Mathf.Max(0f, dividerVerticalInset), Mathf.Max(0f, meterHeight * 0.45f));
        rect.sizeDelta = new Vector2(Mathf.Max(0.5f, dividerWidth), -inset * 2f);
    }

    float ResolveMeterMax()
    {
        // When a voice controller exists, size the meter slightly above loudMin.
        if (voice != null)
            meterMax = voice.loudMin * 1.15f;

        // Always return a positive range so slider math stays valid.
        return Mathf.Max(0.01f, meterMax);
    }

    float GetQuietMax()
    {
        // Use the voice quiet threshold or a simple fallback third of the meter.
        return voice != null ? Mathf.Max(0f, voice.quietMax) : ResolveMeterMax() / 3f;
    }

    float ResolveLoudMin()
    {
        // Use the voice loud threshold or a simple fallback two-thirds point.
        return voice != null ? Mathf.Max(GetQuietMax(), voice.loudMin) : ResolveMeterMax() * (2f / 3f);
    }

    int GetActiveZoneIndex()
    {
        // No controller means no zone should be highlighted.
        if (voice == null)
            return -1;

        // Map the current voice state to its zone array index.
        switch (voice.CurrentState)
        {
            case VoiceActionController.VoiceState.Quiet:
                return 0;
            case VoiceActionController.VoiceState.Medium:
                return 1;
            case VoiceActionController.VoiceState.Loud:
                return 2;
            default:
                return -1;
        }
    }

    Color GetStateColor(VoiceActionController.VoiceState state)
    {
        // Return the configured color for a voice state.
        switch (state)
        {
            case VoiceActionController.VoiceState.Quiet:
                return crouchColor;
            case VoiceActionController.VoiceState.Medium:
                return lightColor;
            case VoiceActionController.VoiceState.Loud:
                return jumpColor;
            case VoiceActionController.VoiceState.Idle:
            default:
                return idleColor;
        }
    }

    static Color WithAlpha(Color color, float alpha)
    {
        // Helper for changing transparency without altering RGB values.
        color.a = alpha;
        return color;
    }

    static void SetRaycastTarget(Graphic graphic, bool enabled)
    {
        // Safely toggle raycast blocking on optional UI graphics.
        if (graphic != null)
            graphic.raycastTarget = enabled;
    }

    private static string GetActionLabel(VoiceActionController.VoiceState state)
    {
        // Convert a gameplay state into the text shown in the optional label.
        switch (state)
        {
            case VoiceActionController.VoiceState.Quiet:
                return "Crouch";
            case VoiceActionController.VoiceState.Medium:
                return "Light";
            case VoiceActionController.VoiceState.Loud:
                return "Jump";
            case VoiceActionController.VoiceState.Idle:
            default:
                return "Idle";
        }
    }
}
