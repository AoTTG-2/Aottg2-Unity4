using UnityEngine;
using Utility;
using Settings;
using UI;
using Weather;
using System.Collections;
using GameProgress;
using Map;
using GameManagers;
using Events;

namespace Cameras
{
    class StaticCamera : BaseCamera
    {
        protected override void Awake()
        {
            base.Awake();
            Camera.backgroundColor = Color.black;
        }
    }
}