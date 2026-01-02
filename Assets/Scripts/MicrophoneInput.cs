using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    public string selectedDevice;
    public int sampleRate = 44100;
    public int sampleWindow = 1024;

    [Range(0f, 1f)] public float smoothing = 0.2f;

    public float RawAmplitude { get; private set; }
    public float SmoothedAmplitude { get; private set; }

    private AudioClip micClip;

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected.");
            enabled = false;
            return;
        }

        selectedDevice = Microphone.devices[0];
        micClip = Microphone.Start(selectedDevice, true, 1, sampleRate);
    }

    void Update()
    {
        RawAmplitude = GetRMS();
        SmoothedAmplitude = Mathf.Lerp(SmoothedAmplitude, RawAmplitude, 1f - smoothing);
    }

    float GetRMS()
    {
        if (micClip == null) return 0f;

        int micPos = Microphone.GetPosition(selectedDevice) - sampleWindow;
        if (micPos < 0) return 0f;

        float[] samples = new float[sampleWindow];
        micClip.GetData(samples, micPos);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        return Mathf.Sqrt(sum / samples.Length);
    }
}