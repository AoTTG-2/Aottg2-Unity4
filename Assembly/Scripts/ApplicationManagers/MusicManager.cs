using UnityEngine;
using System.Collections;
using Utility;
using System.Collections.Generic;
using Events;
using SimpleJSONFixed;
using Settings;

namespace ApplicationManagers
{
    public class MusicManager : MonoBehaviour
    {
        private static MusicManager _instance;
        private static JSONNode _musicInfo;
        private string _currentPlaylist;
        private AudioSource _audio;
        private float _songTimeLeft;
        private float _songVolume;
        private bool _autoPlay;
        private bool _isFading;
        private const float FadeTime = 1.5f;
        private int _currentSong;

        public static void Init()
        {
            _instance = SingletonFactory.CreateSingleton(_instance);
            EventManager.OnFinishInit += OnFinishInit;
            EventManager.OnLoadScene += OnLoadScene;
            _instance._audio = _instance.gameObject.AddComponent<AudioSource>();
        }

        private static void OnFinishInit()
        {
            _musicInfo = JSON.Parse(AssetBundleManager.TryLoadText("MusicInfo"));
        }

        private static void OnLoadScene(SceneName sceneName)
        {
            if (sceneName == SceneName.MainMenu || sceneName == SceneName.MapEditor || sceneName == SceneName.CharacterEditor || sceneName == SceneName.SnapshotViewer)
                SetPlaylist(MusicPlaylist.Menu);
        }

        public static void ApplyGeneralSettings()
        {
            if (_instance != null && !_instance._isFading)
                _instance._audio.volume = _instance._songVolume * GetMusicVolume();
        }

        public static void StopPlaylist()
        {
            _instance._currentPlaylist = string.Empty;
        }

        public static void StopSong()
        {
            SetSong(string.Empty);
        }

        public static void SetPlaylist(string playlist, bool forceNext = false)
        {
            bool change = _instance._currentPlaylist != playlist;
            _instance._currentPlaylist = playlist;
            if (forceNext || change)
            {
                _instance._currentSong = 0;
                NextSong();
            }
        }

        public static void SetSong(string song)
        {
            var songInfo = FindSong(song);
            SetSong(songInfo);
        }

        public static void SetSong(JSONNode songInfo)
        {
            _instance._autoPlay = false;
            AudioClip clip = null;
            float volume = 0f;
            if (songInfo != null)
            {
                if (songInfo.HasKey("Name"))
                {
                    clip = (AudioClip)AssetBundleManager.LoadAsset(songInfo["Name"]);
                    volume = songInfo["Volume"];
                }
               else
                {
                    var playlist = _musicInfo[(string)songInfo["Playlist"]];
                    songInfo = playlist[Random.Range(0, playlist.Count)];
                    clip = (AudioClip)AssetBundleManager.LoadAsset(songInfo["Name"]);
                    volume = songInfo["Volume"];
                }
            }
            _instance.StopAllCoroutines();
            _instance.StartCoroutine(_instance.FadeNextSong(clip, volume));
        }

        public static void NextSong()
        {
            if (_instance._currentPlaylist == string.Empty)
                return;
            JSONNode playlist = _musicInfo[_instance._currentPlaylist];
            JSONNode songInfo;
            if (_instance._currentPlaylist.EndsWith("Ordered"))
            {
                songInfo = playlist[_instance._currentSong];
                _instance._currentSong += 1;
                if (_instance._currentSong >= playlist.Count)
                    _instance._currentSong = 0;
            }
            else
                songInfo = playlist[Random.Range(0, playlist.Count)];
            SetSong(songInfo);
        }

        private static JSONNode FindSong(string name)
        {
            if (name == string.Empty)
                return null;
            foreach (JSONNode playlist in _musicInfo.Values)
            {
                foreach (JSONNode song in playlist)
                {
                    if (song["Name"] == name)
                        return song;
                }
            }
            return null;
        }

        private IEnumerator FadeNextSong(AudioClip nextClip, float volume)
        {
            // fade out
            _isFading = true;
            float fadeTimeLeft = FadeTime;
            if (_audio.isPlaying)
            {
                _songVolume = _audio.volume;
                while (fadeTimeLeft > 0f)
                {
                    float lerp = fadeTimeLeft / FadeTime;
                    _audio.volume = lerp * _songVolume;
                    fadeTimeLeft -= 0.1f;
                    yield return new WaitForSeconds(0.1f);
                }
                _audio.Stop();
            }
            if (nextClip == null)
            {
                _songTimeLeft = 0f;
                _autoPlay = true;
                yield break;
            }

            // set song
            audio.clip = nextClip;
            audio.volume = 0f;
            audio.Play();
            _songTimeLeft = nextClip.length - FadeTime;
            _songVolume = volume;
            _autoPlay = true;

            // fade in
            fadeTimeLeft = FadeTime;
            while (fadeTimeLeft > 0f)
            {
                float lerp = 1f - fadeTimeLeft / FadeTime;
                _audio.volume = lerp * _songVolume * GetMusicVolume();
                fadeTimeLeft -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
            _audio.volume = _songVolume * GetMusicVolume();
            _isFading = false;
        }

        private void Update()
        {
            _songTimeLeft -= Time.deltaTime;
            if (_songTimeLeft <= 0f && _autoPlay)
                NextSong();
        }

        private static float GetMusicVolume()
        {
            return SettingsManager.GeneralSettings.Music.Value * 0.5f;
        }
    }

    public class MusicPlaylist
    {
        public static string Menu = "Menu";
        public static string Default = "Default.Ordered";
        public static string Peaceful = "Peaceful";
        public static string Battle = "Battle";
        public static string Boss = "Boss";
        public static string Racing = "Racing";
    }
}