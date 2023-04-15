using Photon;
using System;
using UnityEngine;
using System.Collections;
using Settings;

namespace Effects {
    class ThunderspearExplodeEffect : BaseEffect
    {
        public static float SizeMultiplier = 1.1f;

        public override void Setup(PhotonPlayer owner, float liveTime, object[] settings)
        {
            base.Setup(owner, liveTime, settings);
            ParticleSystem particle = GetComponent<ParticleSystem>();
            if (SettingsManager.AbilitySettings.UseOldEffect.Value)
            {
                particle.Stop();
                particle.Clear();
                particle = transform.Find("OldExplodeEffect").GetComponent<ParticleSystem>();
                particle.gameObject.SetActive(true);
            }
            else
                particle.startSize *= SizeMultiplier;
            if (SettingsManager.AbilitySettings.ShowBombColors.Value)
            {
                var c = (Color)settings[0];
                particle.startColor = new Color(c.r, c.g, c.b, Mathf.Max(c.a, 0.5f));
            }
        }
    }
}