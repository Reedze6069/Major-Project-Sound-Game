using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MicUI : MonoBehaviour
{
    public MicrophoneInput mic;
    public VoiceActionController voice;

    public Slider meter;
    public Image meterFill;          // Drag Slider/Fill Area/Fill here
    public TMP_Text stateText;

    public float meterMax = 0.5f;    // increase so it doesn’t peg at max

    void Update()
    {
        // Make the UI scale match your gameplay thresholds (so you can see orange/red progress)
        if (voice != null)
        {
            meterMax = voice.loudMin * 1.15f; // a bit of headroom above Loud
        }

        if (mic != null && meter != null)
        {
            meter.minValue = 0f;
            meter.maxValue = meterMax;
            meter.value = Mathf.Min(mic.SmoothedAmplitude, meterMax);
        }

        if (stateText != null && mic != null)
        {
            string v = (voice != null) ? voice.CurrentState.ToString() : "N/A";
            stateText.text = $"Voice: {v} | Raw: {mic.RawAmplitude:F5} | Smooth: {mic.SmoothedAmplitude:F5}";
        }

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
    }

}