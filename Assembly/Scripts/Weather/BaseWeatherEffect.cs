using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Settings;
using System.Collections;

namespace Weather
{
    class BaseWeatherEffect : MonoBehaviour
    {
        protected virtual Vector3 _positionOffset => Vector3.zero;
        protected virtual float _audioFadeTime => 2f;
        protected Transform _parent;
        protected Transform _transform;
        protected float _level;
        protected float _maxParticles;
        protected float _particleMultiplier;
        protected List<ParticleEmitter> _particleEmitters = new List<ParticleEmitter>();
        protected List<ParticleSystem> _particleSystems = new List<ParticleSystem>();
        protected List<AudioSource> _audioSources = new List<AudioSource>();
        protected Dictionary<AudioSource, float> _audioTargetVolumes = new Dictionary<AudioSource, float>();
        protected Dictionary<AudioSource, float> _audioStartTimes = new Dictionary<AudioSource, float>();
        protected Dictionary<AudioSource, float> _audioStartVolumes = new Dictionary<AudioSource, float>();
        protected bool _isDisabling = false;

        public virtual void Disable(bool fadeOut = false)
        {
            if (gameObject.activeSelf)
            {
                if (fadeOut)
                {
                    if (!_isDisabling)
                        StartCoroutine(WaitAndDisable());
                }
                else
                {
                    StopAllCoroutines();
                    StopAllAudio(fadeOut: false);
                    StopAllEmitters();
                    StopAllParticleSystems();
                    gameObject.SetActive(false);
                    _isDisabling = false;
                }
            }
        }

        public virtual void Enable()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                _isDisabling = false;
            }
        }

        private IEnumerator WaitAndDisable()
        {
            _isDisabling = true;
            StopAllAudio(fadeOut: true);
            StopAllEmitters();
            StopAllParticleSystems();
            yield return new WaitForSeconds(_audioFadeTime);
            gameObject.SetActive(false);
            _isDisabling = false;
        }

        public virtual void Randomize()
        {
        }

        public virtual void SetParent(Transform parent)
        {
            _parent = parent;
        }

        public virtual void SetLevel(float level)
        {
            _level = level;
        }

        public virtual void Setup(Transform parent)
        {
            _transform = transform;
            _parent = parent;
            if (SettingsManager.GraphicsSettings.WeatherEffects.Value == (int)WeatherEffectLevel.High)
            {
                _maxParticles = 500f;
                _particleMultiplier = 1f;
            }
            else
            {
                _maxParticles = 200f;
                _particleMultiplier = 0.7f;
            }
            _particleEmitters = GetComponentsInChildren<ParticleEmitter>().OrderBy(x => x.gameObject.name).ToList();
            _particleSystems = GetComponentsInChildren<ParticleSystem>().OrderBy(x => x.gameObject.name).ToList();
            _audioSources = GetComponentsInChildren<AudioSource>().OrderBy(x => x.gameObject.name).ToList();
            foreach (ParticleEmitter emitter in _particleEmitters)
            {
                emitter.emit = false;
                emitter.transform.localPosition = _positionOffset;
                emitter.transform.localRotation = Quaternion.identity;
            }
            foreach (ParticleSystem system in _particleSystems)
            {
                system.Stop();
                system.transform.localPosition = _positionOffset;
                system.transform.localRotation = Quaternion.identity;
            }
            StopAllAudio();
        }

        protected virtual void SetActiveEmitter(int index)
        {
            StopAllEmitters();
            StopAllParticleSystems();
            _particleEmitters[index].emit = true;
        }

        protected virtual void StopAllEmitters()
        {
            foreach (ParticleEmitter emitter in _particleEmitters)
                emitter.emit = false;
        }

        protected virtual void SetActiveParticleSystem(int index)
        {
            StopAllEmitters();
            StopAllParticleSystems();
            if (!_particleSystems[index].isPlaying)
                _particleSystems[index].Play();
        }

        protected virtual void StopAllParticleSystems()
        {
            foreach (ParticleSystem system in _particleSystems)
                system.Stop();
        }

        protected virtual void SetActiveAudio(int index, float volume)
        {
            for (int i = 0; i < _audioSources.Count; i++)
            {
                if (i == index)
                    SetAudioVolume(i, volume);
                else
                    SetAudioVolume(i, 0f);
            }
        }

        protected virtual void SetAudioVolume(int index, float volume)
        {
            SetAudioVolume(_audioSources[index], volume);
        }

        protected virtual void SetAudioVolume(AudioSource audio, float volume)
        {
            volume = Mathf.Clamp(volume, 0f, 1f);
            if (_audioTargetVolumes[audio] != volume)
            {
                _audioTargetVolumes[audio] = volume;
                if (volume == 0f)
                {
                    _audioStartTimes[audio] = Time.time;
                    _audioStartVolumes[audio] = audio.volume;
                }
            }
        }

        protected virtual void StopAllAudio(bool fadeOut = false)
        {
            if (fadeOut)
            {
                foreach (AudioSource audio in _audioSources)
                    SetAudioVolume(audio, 0f);
            }
            else
            {
                foreach (AudioSource audio in _audioSources)
                {
                    audio.Stop();
                    _audioTargetVolumes[audio] = 0f;
                    _audioStartTimes[audio] = 0f;
                    _audioStartVolumes[audio] = 0f;
                }
            }
        }

        protected virtual float ClampParticles(float count)
        {
            return Mathf.Min(count * _particleMultiplier, _maxParticles);
        }

        protected virtual void LateUpdate()
        {
            _transform.position = _parent.position;
            UpdateAudio();
        }

        protected virtual void UpdateAudio()
        {
            foreach (AudioSource audio in _audioSources)
            {
                if (_audioTargetVolumes[audio] == 0f)
                {
                    if (audio.isPlaying)
                    {
                        audio.volume = GetLerpedVolume(audio);
                        if (audio.volume == 0f)
                            audio.Pause();
                    }
                }
                else
                {
                    if (audio.isPlaying)
                    {
                        if (audio.volume != _audioTargetVolumes[audio])
                            audio.volume = GetLerpedVolume(audio);
                    }
                    else
                    {
                        _audioStartTimes[audio] = Time.time;
                        _audioStartVolumes[audio] = 0f;
                        audio.volume = GetLerpedVolume(audio);
                        audio.Play();
                    }
                }
            }
        }

        protected virtual float GetLerpedVolume(AudioSource audio)
        {
            float lerp = (Time.time - _audioStartTimes[audio]) / _audioFadeTime;
            lerp = Mathf.Clamp(lerp, 0f, 1f);
            float volume = Mathf.Lerp(audio.volume, _audioTargetVolumes[audio], lerp);
            return volume;
        }
    }
}
