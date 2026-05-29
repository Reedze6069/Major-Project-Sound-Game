using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    // Optional device name; if empty or invalid, the first microphone is used.
    public string selectedDevice;
    // Requested microphone sample rate.
    public int sampleRate = 44100;
    // Number of recent samples used when measuring volume.
    public int sampleWindow = 1024;

    // Amount of smoothing applied to raw microphone amplitude.
    [Range(0f, 1f)] public float smoothing = 0.2f;
    // Multiplier used to make quiet microphones easier to detect.
    [Min(0.1f)] public float inputGain = 2f;

    // Latest unsmoothed microphone volume.
    public float RawAmplitude { get; private set; }
    // Smoothed volume used by gameplay systems.
    public float SmoothedAmplitude { get; private set; }
    // Volume value exposed for UI display.
    public float DisplayAmplitude { get; private set; }
    // Volume value exposed for gameplay decisions.
    public float EffectiveAmplitude { get; private set; }

    // AudioClip that Unity records microphone input into.
    private AudioClip micClip;
    // Reused buffer that stores the current analysis window.
    private float[] sampleBuffer;

    void Start()
    {
        // Disable this component if the computer has no microphone.
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected.");
            enabled = false;
            return;
        }

        // Pick the first available microphone if the chosen device is missing.
        if (string.IsNullOrWhiteSpace(selectedDevice) || System.Array.IndexOf(Microphone.devices, selectedDevice) < 0)
            selectedDevice = Microphone.devices[0];

        // Allocate the analysis buffer and start recording a looping mic clip.
        sampleBuffer = new float[Mathf.Max(64, sampleWindow)];
        micClip = Microphone.Start(selectedDevice, true, 1, sampleRate);
    }

    void Update()
    {
        // Read the current volume, apply gain, then smooth it for stable controls.
        RawAmplitude = AnalyzeCurrentWindow() * inputGain;
        SmoothedAmplitude = Mathf.Lerp(SmoothedAmplitude, RawAmplitude, 1f - smoothing);
        // Keep separate output properties in case UI and gameplay diverge later.
        DisplayAmplitude = SmoothedAmplitude;
        EffectiveAmplitude = SmoothedAmplitude;
    }

    float AnalyzeCurrentWindow()
    {
        // No clip or invalid window means there is no usable input yet.
        if (micClip == null || sampleWindow <= 0)
            return 0f;

        // Read the most recent complete block of microphone samples.
        int micPos = Microphone.GetPosition(selectedDevice) - sampleWindow;
        if (micPos < 0)
            return 0f;

        // Rebuild the buffer if the inspector sample size changed.
        if (sampleBuffer == null || sampleBuffer.Length != sampleWindow)
            sampleBuffer = new float[sampleWindow];

        micClip.GetData(sampleBuffer, micPos);

        // Sum squared samples so positive and negative wave values both count.
        float sum = 0f;

        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            float sample = sampleBuffer[i];
            sum += sample * sample;
        }

        // Return RMS volume, which is a stable measure of signal strength.
        return Mathf.Sqrt(sum / sampleBuffer.Length);
    }
}
