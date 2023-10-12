using Photon;
using System;
using UnityEngine;
using System.Collections;
using Settings;

namespace Effects {
    class LineRendererEffect : BaseEffect
    {
        protected float _totalTime;
        protected LineRenderer _renderer;

        public override void Setup(PhotonPlayer owner, float liveTime, object[] settings)
        {
            base.Setup(owner, liveTime, settings);
            _renderer = GetComponent<LineRenderer>();
            _renderer.SetVertexCount(2);
            _renderer.SetPosition(0, (Vector3)settings[0]);
            _renderer.SetPosition(1, (Vector3)settings[1]);
            _renderer.SetWidth((float)settings[2], (float)settings[3]);
            _totalTime = (float)settings[4];
            _timeLeft = _totalTime;
        }

        protected override void Update()
        {
            base.Update();
            Color color = new Color(1f, 1f, 1f, _timeLeft / _totalTime);
            _renderer.SetColors(color, color);
        }
    }
}