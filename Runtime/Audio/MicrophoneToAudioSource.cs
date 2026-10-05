using UnityEngine;

namespace Skote.Vfx.Audio
{
[RequireComponent(typeof(AudioSource))]
public class MicrophoneToAudioSource : MonoBehaviour
{
    public bool autoStart = true; 
    public int micSampleRate = 44100; 

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (autoStart) StartMic();
    }

    public void StartMic()
    {
        AudioClip clip = Microphone.Start(null, true, 1, micSampleRate);
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void StopMic()
    {
        audioSource.Stop();
        Microphone.End(null);
    }
}
}