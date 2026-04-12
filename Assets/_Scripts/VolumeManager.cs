using UnityEngine;
using UnityEngine.Audio;

public class VolumeManager : MonoBehaviour
{
    public AudioMixer mainMixer;

    void Start()
    {
        // Set volume to 0.5 on game start
        SetSFXVolume(0.5f);
    }

    public void SetSFXVolume(float sliderValue)
    {
        float dBValue = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20;
        mainMixer.SetFloat("SFXVolume", dBValue);
    }
}