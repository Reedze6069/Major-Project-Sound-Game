using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    public string selectedDevice;
    public int sampleRate = 44100;
    public int sampleWindow = 1024;

    [Range(0f, 1f)] public float smoothing = 0.2f;
    [Min(0.1f)] public float inputGain = 2f;

    public float RawAmplitude { get; private set; }
    public float SmoothedAmplitude { get; private set; }
    public float DisplayAmplitude { get; private set; }
    public float EffectiveAmplitude { get; private set; }

    private AudioClip micClip;
    private float[] sampleBuffer;

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected.");
            enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedDevice) || System.Array.IndexOf(Microphone.devices, selectedDevice) < 0)
            selectedDevice = Microphone.devices[0];

        sampleBuffer = new float[Mathf.Max(64, sampleWindow)];
        micClip = Microphone.Start(selectedDevice, true, 1, sampleRate);
    }

    void Update()
    {
        RawAmplitude = AnalyzeCurrentWindow() * inputGain;
        SmoothedAmplitude = Mathf.Lerp(SmoothedAmplitude, RawAmplitude, 1f - smoothing);
        DisplayAmplitude = SmoothedAmplitude;
        EffectiveAmplitude = SmoothedAmplitude;
    }

    float AnalyzeCurrentWindow()
    {
        if (micClip == null || sampleWindow <= 0)
            return 0f;

        int micPos = Microphone.GetPosition(selectedDevice) - sampleWindow;
        if (micPos < 0)
            return 0f;

        if (sampleBuffer == null || sampleBuffer.Length != sampleWindow)
            sampleBuffer = new float[sampleWindow];

        micClip.GetData(sampleBuffer, micPos);

        float sum = 0f;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            float sample = sampleBuffer[i];
            sum += sample * sample;
        }

        return Mathf.Sqrt(sum / sampleBuffer.Length);
    }
}
