using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Settings;
using ApplicationManagers;
using Utility;

namespace Weather
{
    class SnowWeatherEffect : BaseWeatherEffect
    {
        protected override Vector3 _positionOffset => Vector3.up * 0f;

        public override void Randomize()
        {
            _particleEmitters[0].rndVelocity = new Vector3(20f, 20f, 20f);
            _particleEmitters[0].minEnergy = _particleEmitters[0].maxEnergy = 1.2f;
            _particleEmitters[1].rndVelocity = new Vector3(5f, 5f, 5f);
            _particleEmitters[1].localVelocity = new Vector3(20f * RandomGen.GetRandomSign(), 0f, 0f);
            _particleEmitters[1].minEnergy = _particleEmitters[0].maxEnergy = 1.2f;

        }

        public override void SetLevel(float level)
        {
            base.SetLevel(level);
            if (level <= 0f)
            {
                return;
            }
            if (level <= 0.5f)
            {
                float scale = level / 0.5f;
                SetActiveEmitter(0);
                SetActiveAudio(0, 0.25f + 0.25f * scale);
                _particleEmitters[0].minEmission = _particleEmitters[0].maxEmission = ClampParticles(100f + scale * 300f);
                _particleEmitters[0].minSize = _particleEmitters[0].maxSize = 25f;
            }
            else
            {
                float scale = (level - 0.5f) / 0.5f;
                SetActiveEmitter(1);
                SetAudioVolume(1, 0.25f + 0.25f * scale);
                _particleEmitters[1].minEmission = _particleEmitters[1].maxEmission = ClampParticles(200f + scale * 200f);
                _particleEmitters[1].minSize = _particleEmitters[1].maxSize = 12f;
            }
        }

        public override void Setup(Transform parent)
        {
            base.Setup(parent);
        }
    }
}
