using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Settings;
using ApplicationManagers;

namespace Weather
{
    class RainWeatherEffect : BaseWeatherEffect
    {
        protected override Vector3 _positionOffset => Vector3.up * 30f;

        public override void Randomize()
        {
            float angle1 = Random.Range(0f, 20f);
            angle1 = Random.Range(-angle1, angle1);
            foreach (ParticleEmitter emitter in _particleEmitters)
            {
                emitter.transform.localPosition = _positionOffset;
                emitter.transform.localRotation = Quaternion.identity;
                emitter.transform.RotateAround(_transform.position, Vector3.forward, angle1);
                emitter.transform.RotateAround(_transform.position, Vector3.up, Random.Range(0f, 360f));
            }
        }

        public override void SetLevel(float level)
        {
            base.SetLevel(level);
            if (level <= 0f)
                return;
            if (level < 0.5f)
            {
                float scale = level / 0.5f;
                SetActiveEmitter(0);
                _particleEmitters[0].minEmission = _particleEmitters[0].maxEmission = ClampParticles(50f + (150f * scale));
                _particleEmitters[0].minSize = _particleEmitters[0].maxSize = 30f + 30f * scale;
                SetActiveAudio(0, 0.25f + 0.25f * scale);
            }
            else
            {
                float scale = (level - 0.5f) / 0.5f;
                SetActiveEmitter(1);
                _particleEmitters[1].minEmission = _particleEmitters[1].maxEmission = ClampParticles(100f + (150f * scale));
                _particleEmitters[1].minSize = _particleEmitters[1].maxSize = 50f + scale * 10f;
                SetActiveAudio(1, 0.25f + 0.25f * scale);
            }
        }

        public override void Setup(Transform parent)
        {
            base.Setup(parent);
            _particleEmitters[0].localVelocity = Vector3.down * 100f;
            _particleEmitters[1].localVelocity = Vector3.down * 100f;
            _particleEmitters[0].rndVelocity = new Vector3(10f, 0f, 10f);
            _particleEmitters[1].rndVelocity = new Vector3(10f, 0f, 10f);
        }
    }
}
