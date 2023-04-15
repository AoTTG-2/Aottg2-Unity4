using System.Collections.Generic;
using UnityEngine;
using Weather;
using UI;
using Utility;
using CustomSkins;
using ApplicationManagers;
using Photon;
using Map;
using CustomLogic;
using System.Collections;

namespace GameManagers
{
    class BaseGameManager : Photon.MonoBehaviour
    {
        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {
            StartCoroutine(WaitAndLoad());
        }

        protected IEnumerator WaitAndLoad()
        {
            while (!IsFinishedLoading())
                yield return null;
            OnFinishLoading();
            SceneLoader.CurrentCamera.OnFinishLoading();
            WeatherManager.OnFinishLoading();
        }

        public virtual bool IsFinishedLoading()
        {
            return CustomLogicManager.LogicLoaded && MapManager.MapLoaded;
        }

        protected virtual void OnFinishLoading()
        {
        }
    }
}
