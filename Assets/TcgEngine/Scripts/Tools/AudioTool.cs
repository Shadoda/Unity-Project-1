using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{

    /// <summary>
    /// Main audio script, allow to play sounds by channel
    /// </summary>

    public class AudioTool : MonoBehaviour
    {
        private static AudioTool instance;

        private Dictionary<string, AudioSource> channels_sfx = new Dictionary<string, AudioSource>();
        private Dictionary<string, AudioSource> channels_music = new Dictionary<string, AudioSource>();
        private Dictionary<string, float> channels_volume = new Dictionary<string, float>();
        private Dictionary<string, float> tchannels_volume = new Dictionary<string, float>();

        [HideInInspector] public float master_vol = 1f;
        [HideInInspector] public float sfx_vol = 1f;
        [HideInInspector] public float music_vol = 1f;

        private void Awake()
        {
            RefreshVolume();
        }

        private void Update()
        {
            foreach (KeyValuePair<string, AudioSource> pair in channels_music)
            {
                if (pair.Value.isPlaying)
                {
                    float tvol = tchannels_volume[pair.Key];
                    float vol = channels_volume[pair.Key];
                    vol = Mathf.MoveTowards(vol, tvol, 0.5f * Time.deltaTime);
                    channels_volume[pair.Key] = vol;
                    pair.Value.volume = vol * music_vol;

                    if (vol < 0.01f && tvol < 0.01f)
                        StopMusic(pair.Key);
                }
            }

            foreach (KeyValuePair<string, AudioSource> pair in channels_sfx)
            {
                if (pair.Value.isPlaying)
                {
                    float tvol = tchannels_volume[pair.Key];
                    float vol = channels_volume[pair.Key];
                    vol = Mathf.MoveTowards(vol, tvol, 0.5f * Time.deltaTime);
                    channels_volume[pair.Key] = vol;
                    pair.Value.volume = vol * sfx_vol;

                    if (vol < 0.01f && tvol < 0.01f)
                        StopSFX(pair.Key);
                }
            }
        }

        //channel: Two sounds on the same channel will never play at the same time, sounds on different channel will play at the same time.
        //priority: if false, will not play if a sound is already playing on the channel, if true, will replace current sound playing on channel
        public void PlaySFX(string channel, AudioClip sound, float vol = 0.6f, bool priority = true, bool loop = false)
        {
            if (string.IsNullOrEmpty(channel) || sound == null)
                return;

            AudioSource source = GetChannel(channel);
            channels_volume[channel] = vol;
            tchannels_volume[channel] = vol;

            if (source == null)
            {
                source = CreateChannel(channel); //Create channel if doesnt exist, for optimisation put the channel in preload_channels so its created at start instead of here
                channels_sfx[channel] = source;
            }

            if (source)
            {
                if (priority || !source.isPlaying)
                {
                    source.clip = sound;
                    source.volume = vol * sfx_vol;
                    source.loop = loop;
                    source.Play();
                }
            }
        }

        //channel: Two sounds on the same channel will never play at the same time, sounds on different channel will play at the same time.
        //If music is already playing on the same channel, new music will be played unless its the same one.(Won't restart in that case)
        public void PlayMusic(string channel, AudioClip music, float vol = 0.3f, bool loop = true)
        {
            if (string.IsNullOrEmpty(channel) || music == null)
                return;

            AudioSource source = GetMusicChannel(channel);
            channels_volume[channel] = vol;
            tchannels_volume[channel] = vol;

            if (source == null)
            {
                source = CreateChannel(channel); //Create channel if doesnt exist, for optimisation put the channel in preload_channels so its created at start instead of here
                channels_music[channel] = source;
            }

            if (source)
            {
                if (!source.isPlaying || source.clip != music)
                {
                    source.clip = music;
                    source.volume = vol * music_vol;
                    source.loop = loop;
                    source.Play();
                }
            }
        }

        public void StopSFX(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return;

            AudioSource source = GetChannel(channel);
            if (source)
            {
                source.Stop();
            }
        }

        public void StopMusic(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return;

            AudioSource source = GetMusicChannel(channel);
            if (source)
            {
                source.Stop();
            }
        }

        public void FadeOutMusic(string channel)
        {
            if (tchannels_volume.ContainsKey(channel))
                tchannels_volume[channel] = 0f;
        }

        public void FadeOutSFX(string channel)
        {
            if (tchannels_volume.ContainsKey(channel))
                tchannels_volume[channel] = 0f;
        }

        public void SetMasterVolume(float value)
        {
            master_vol = value;
            PlayerPrefs.SetFloat("audio_master_volume", master_vol);
            RefreshVolume();
        }

        public void SetMusicVolume(float value)
        {
            music_vol = value;
            PlayerPrefs.SetFloat("audio_music_volume", music_vol);
            RefreshVolume();
        }

        public void SetSFXVolume(float value)
        {
            sfx_vol = value;
            PlayerPrefs.SetFloat("audio_sfx_volume", sfx_vol);
            RefreshVolume();
        }

        public void RefreshVolume()
        {

            AudioListener.volume = master_vol;

            foreach (KeyValuePair<string, AudioSource> pair in channels_sfx)
            {
                if (pair.Value != null)
                {
                    float vol = channels_volume.ContainsKey(pair.Key) ? channels_volume[pair.Key] : 0.8f;
                    pair.Value.volume = vol * sfx_vol;
                }
            }

            foreach (KeyValuePair<string, AudioSource> pair in channels_music)
            {
                if (pair.Value != null)
                {
                    float vol = channels_volume.ContainsKey(pair.Key) ? channels_volume[pair.Key] : 0.4f;
                    pair.Value.volume = vol * music_vol;
                }
            }
        }

        public bool IsMusicPlaying(string channel)
        {
            AudioSource source = GetMusicChannel(channel);
            if (source != null)
                return source.isPlaying;
            return false;
        }

        public AudioSource CreateChannel(string channel, int priority = 128)
        {
            if (string.IsNullOrEmpty(channel))
                return null;

            GameObject cobj = new GameObject("AudioChannel-" + channel);
            cobj.transform.SetParent(transform);
            AudioSource caudio = cobj.AddComponent<AudioSource>();
            caudio.playOnAwake = false;
            caudio.loop = false;
            caudio.priority = priority;
            return caudio;
        }

        public AudioSource GetChannel(string channel)
        {
            if (channels_sfx.ContainsKey(channel))
                return channels_sfx[channel];
            return null;
        }

        public AudioSource GetMusicChannel(string channel)
        {
            if (channels_music.ContainsKey(channel))
                return channels_music[channel];
            return null;
        }

        public bool DoesChannelExist(string channel)
        {
            return channels_sfx.ContainsKey(channel);
        }

        public bool DoesMusicChannelExist(string channel)
        {
            return channels_music.ContainsKey(channel);
        }

        public float GetMasterVolume()
        {
            return master_vol;
        }

        public float GetSFXVolume()
        {
            return sfx_vol;
        }

        public float GetMusicVolume()
        {
            return music_vol;
        }

        public static AudioTool Get()
        {
            if (instance == null)
            {
                GameObject audio_system = new GameObject("AudioSystem");
                instance = audio_system.AddComponent<AudioTool>();
                DontDestroyOnLoad(audio_system);
            }
            return instance;
        }
    }
}

