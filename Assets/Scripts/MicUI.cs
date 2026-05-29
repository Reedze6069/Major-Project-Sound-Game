using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MicUI : MonoBehaviour
{
    public MicrophoneInput mic;
    public VoiceActionController voice;

    [Header("Meter")]
    public Slider meter;
    public Image meterFill;
    public TMP_Text stateText;

    public float meterMax = 0.5f;
    [Range(0.01f, 0.5f)] public float meterSmoothTime = 0.12f;

    [Header("Compact Layout")]
    [Min(12f)] public float meterHeight = 24f;
    [Min(1f)] public float handleIndicatorWidth = 4f;
    [Range(0f, 1f)] public float handleIndicatorAlpha = 0.5f;
    [Min(0.5f)] public float dividerWidth = 1f;
    [Min(0f)] public float dividerVerticalInset = 4f;

    [Header("Zone Styling")]
    public bool hideStandaloneStateText = true;
    [Range(0f, 1f)] public float inactiveZoneAlpha = 0.1f;
    [Range(0f, 1f)] public float activeZoneAlpha = 0.42f;
    [Range(0f, 1f)] public float inactiveLabelAlpha = 0.55f;
    [Range(0f, 1f)] public float activeLabelAlpha = 0.95f;
    public Color idleColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    public Color crouchColor = new Color(0.24f, 0.86f, 0.34f, 1f);
    public Color lightColor = new Color(1f, 0.42f, 0.05f, 1f);
    public Color jumpColor = new Color(1f, 0.32f, 0.32f, 1f);
    public Color dividerColor = new Color(1f, 1f, 1f, 0.12f);

    float displayedMeterValue;
    float meterVelocity;

    RectTransform zoneBackdrop;
    RectTransform zoneLabelOverlay;
    readonly Image[] zoneBackgrounds = new Image[3];
    readonly TMP_Text[] zoneLabels = new TMP_Text[3];
    readonly Image[] zoneDividers = new Image[2];

    void Awake()
    {
        EnsureMeterZones();
    }

    void Update()
    {
        EnsureMeterZones();

        if (meter != null)
        {
            meter.minValue = 0f;
            meter.maxValue = ResolveMeterMax();

            float smoothedAmplitude = mic != null ? mic.SmoothedAmplitude : 0f;
            float targetValue = Mathf.Min(smoothedAmplitude, meter.maxValue);
            displayedMeterValue = Mathf.SmoothDamp(displayedMeterValue, targetValue, ref meterVelocity, meterSmoothTime);
            meter.value = displayedMeterValue;
        }

        UpdateMeterColors();
        UpdateZoneLayout();
        UpdateZoneColors();
        UpdateStateText();
    }

    void EnsureMeterZones()
    {
        if (meter == null)
            return;

        RectTransform meterRect = meter.transform as RectTransform;
        if (meterRect == null)
            return;

        ApplyCompactLayout(meterRect);

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
    }

    void UpdateMeterColors()
    {
        if (meterFill == null)
            return;

        VoiceActionController.VoiceState currentState = voice != null
            ? voice.CurrentState
            : VoiceActionController.VoiceState.Idle;

        Color tintedFill = Color.Lerp(Color.white, GetStateColor(currentState), 0.25f);
        meterFill.color = WithAlpha(tintedFill, currentState == VoiceActionController.VoiceState.Idle ? 0.14f : 0.28f);
    }

    void UpdateZoneLayout()
    {
        if (zoneBackgrounds[0] == null || zoneLabels[0] == null)
            return;

        float resolvedMeterMax = ResolveMeterMax();
        float quietEnd = Mathf.Clamp01(GetQuietMax() / resolvedMeterMax);
        float loudStart = Mathf.Clamp(ResolveLoudMin() / resolvedMeterMax, quietEnd, 1f);

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
        Color[] colors =
        {
            crouchColor,
            lightColor,
            jumpColor
        };

        int activeIndex = GetActiveZoneIndex();

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

        for (int i = 0; i < zoneDividers.Length; i++)
        {
            if (zoneDividers[i] != null)
                zoneDividers[i].color = dividerColor;
        }
    }

    void UpdateStateText()
    {
        if (stateText == null)
            return;

        stateText.gameObject.SetActive(!hideStandaloneStateText);
        if (!hideStandaloneStateText)
            stateText.text = $"Action: {GetActionLabel(voice != null ? voice.CurrentState : VoiceActionController.VoiceState.Idle)}";
    }

    void ApplyCompactLayout(RectTransform meterRect)
    {
        float resolvedHeight = Mathf.Max(12f, meterHeight);
        meterRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, resolvedHeight);

        meter.interactable = false;
        meter.transition = Selectable.Transition.None;

        SetRaycastTarget(meterFill, false);
        SetRaycastTarget(meter.targetGraphic, false);

        Transform background = meter.transform.Find("Background");
        Image backgroundImage = background != null ? background.GetComponent<Image>() : null;
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0f, 0f, 0f, 0.18f);
            backgroundImage.raycastTarget = false;
        }

        if (meter.handleRect == null)
            return;

        RectTransform handleRect = meter.handleRect;
        handleRect.anchorMin = new Vector2(handleRect.anchorMin.x, 0.18f);
        handleRect.anchorMax = new Vector2(handleRect.anchorMax.x, 0.82f);
        handleRect.sizeDelta = new Vector2(Mathf.Max(1f, handleIndicatorWidth), 0f);

        Image handleImage = handleRect.GetComponent<Image>();
        if (handleImage != null)
        {
            handleImage.color = WithAlpha(Color.white, handleIndicatorAlpha);
            handleImage.raycastTarget = false;
        }
    }

    RectTransform FindOrCreateLayer(RectTransform parent, string name, int siblingIndex)
    {
        Transform existing = parent.Find(name);
        RectTransform layer;

        if (existing != null)
        {
            layer = existing as RectTransform;
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            layer = go.GetComponent<RectTransform>();
            layer.SetParent(parent, false);
        }

        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = Vector2.zero;
        layer.offsetMax = Vector2.zero;
        layer.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
        return layer;
    }

    void CreateZoneBackground(int index, string name, Transform parent)
    {
        if (zoneBackgrounds[index] != null)
            return;

        Transform existing = parent.Find(name);
        Image image;

        if (existing != null)
        {
            image = existing.GetComponent<Image>();
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>();
        }

        image.raycastTarget = false;
        zoneBackgrounds[index] = image;
    }

    void CreateZoneLabel(int index, string name, string labelText, Transform parent)
    {
        if (zoneLabels[index] != null)
            return;

        Transform existing = parent.Find(name);
        TextMeshProUGUI label;

        if (existing != null)
        {
            label = existing.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            label = go.GetComponent<TextMeshProUGUI>();
        }

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
        if (zoneDividers[index] != null)
            return;

        Transform existing = parent.Find(name);
        Image divider;

        if (existing != null)
        {
            divider = existing.GetComponent<Image>();
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            divider = go.GetComponent<Image>();
        }

        divider.raycastTarget = false;
        zoneDividers[index] = divider;
    }

    void SetStretch(RectTransform rect, float start, float end)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(start, 0f);
        rect.anchorMax = new Vector2(end, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void SetDivider(Image divider, float position)
    {
        if (divider == null)
            return;

        RectTransform rect = divider.rectTransform;
        rect.anchorMin = new Vector2(position, 0f);
        rect.anchorMax = new Vector2(position, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        float inset = Mathf.Min(Mathf.Max(0f, dividerVerticalInset), Mathf.Max(0f, meterHeight * 0.45f));
        rect.sizeDelta = new Vector2(Mathf.Max(0.5f, dividerWidth), -inset * 2f);
    }

    float ResolveMeterMax()
    {
        if (voice != null)
            meterMax = voice.loudMin * 1.15f;

        return Mathf.Max(0.01f, meterMax);
    }

    float GetQuietMax()
    {
        return voice != null ? Mathf.Max(0f, voice.quietMax) : ResolveMeterMax() / 3f;
    }

    float ResolveLoudMin()
    {
        return voice != null ? Mathf.Max(GetQuietMax(), voice.loudMin) : ResolveMeterMax() * (2f / 3f);
    }

    int GetActiveZoneIndex()
    {
        if (voice == null)
            return -1;

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
        color.a = alpha;
        return color;
    }

    static void SetRaycastTarget(Graphic graphic, bool enabled)
    {
        if (graphic != null)
            graphic.raycastTarget = enabled;
    }

    private static string GetActionLabel(VoiceActionController.VoiceState state)
    {
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
