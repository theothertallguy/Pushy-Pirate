using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicClass : GenericSingleton<MusicClass>
{
    private AudioSource _audioSource;
    [SerializeField]
    public AudioClip AudioClip1;
    [SerializeField]
    public AudioClip AudioClip2;
    [SerializeField]
    public AudioClip AudioClip3;
    private bool isFocused;
    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void OnApplicationFocus(bool b)
    {
        //Debug.Log("application focus: " + b);
        isFocused = b;
    }

 // Update is called once per frame
    void Update()
    {
        if (isFocused && !_audioSource.isPlaying)
            playNextMusic();
    }

    private void playNextMusic()
    {
        if (_audioSource.clip == AudioClip1) {
            _audioSource.clip = AudioClip2;
        } else if (_audioSource.clip == AudioClip2) {
            _audioSource.clip = AudioClip3;
        } else if (_audioSource.clip == AudioClip3) {
            _audioSource.clip = AudioClip1;
        }
        PlayMusic();
    }

    public void PlayMusic()
    {
        if (_audioSource.isPlaying) return;
        _audioSource.Play();
    }

    public void StopMusic()
    {
        _audioSource.Stop();
    }
}
