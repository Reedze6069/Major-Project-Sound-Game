using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MicUI : MonoBehaviour
{
    public MicrophoneInput mic;
    public VoiceActionController voice;

    public Slider meter;
    public Image meterFill;
    public TMP_Text stateText;

    public float meterMax = 0.5f;
    [Range(0.01f, 0.5f)] public float meterSmoothTime = 0.12f;

    private float displayedMeterValue;
    private float meterVelocity;

    void Update()
    {
        if (meterFill != null && voice != null)
        {
            switch (voice.CurrentState)
            {
                case VoiceActionController.VoiceState.Neutral:
                    meterFill.color = Color.gray;
                    break;
                case VoiceActionController.VoiceState.Quiet:
                    meterFill.color = Color.green;
                    break;
                case VoiceActionController.VoiceState.Medium:
                    meterFill.color = new Color(1f, 0.55f, 0f);
                    break;
                case VoiceActionController.VoiceState.Loud:
                    meterFill.color = Color.red;
                    break;
            }
        }

        if (mic != null && meter != null)
        {
            meter.minValue = 0f;
            meter.maxValue = meterMax;

            if (voice != null)
            {
                meterMax = voice.loudMin * 1.15f;
                meter.maxValue = meterMax;
            }

            float targetValue = Mathf.Min(mic.SmoothedAmplitude, meterMax);
            displayedMeterValue = Mathf.SmoothDamp(displayedMeterValue, targetValue, ref meterVelocity, meterSmoothTime);
            meter.value = displayedMeterValue;
        }

        if (stateText != null && mic != null)
        {
            string v = voice != null ? voice.CurrentState.ToString() : "N/A";
            stateText.text = $"Voice: {v}";
        }
    }
}
