using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xft;
using CustomSkins;
using Settings;
using UI;
using ApplicationManagers;
using Weather;
using GameProgress;
using GameManagers;
using Controllers;

namespace Characters
{
    class HumanDummy: MonoBehaviour
    {
        public HumanComponentCache Cache;
        public HumanState State = HumanState.Idle;
        public HumanSetup Setup;

        // actions
        private float _stateTimeLeft = 0f;

        protected void Awake()
        {
            Cache = new HumanComponentCache(gameObject);
            Cache.Rigidbody.freezeRotation = true;
            Cache.Rigidbody.useGravity = false;
            Cache.Rigidbody.velocity = Vector3.zero;
            Setup = gameObject.AddComponent<HumanSetup>();
            Destroy(gameObject.GetComponentInChildren<SmoothSyncMovement>());
        }

        protected void Start()
        {
        }

        public void Idle()
        {
            State = HumanState.Idle;
            string animation = HumanAnimations.StandFemale;
            if (Setup.Weapon == HumanWeapon.Gun)
                animation = HumanAnimations.StandGun;
            else if (Setup.CustomSet.Sex.Value == (int)HumanSex.Male)
                animation = HumanAnimations.StandMale;
            Cache.Animation.CrossFade(animation, 0.1f);
        }

        public void EmoteAction(string emote)
        {
            string animation = HumanAnimations.Salute;
            if (emote == "Salute")
                animation = HumanAnimations.Salute;
            else if (emote == "Dance")
                animation = HumanAnimations.SpecialArmin;
            else if (emote == "Flip")
                animation = HumanAnimations.Dodge;
            else if (emote == "Wave1")
                animation = HumanAnimations.SpecialMarco0;
            else if (emote == "Wave2")
                animation = HumanAnimations.SpecialMarco1;
            else if (emote == "Eat")
                animation = HumanAnimations.SpecialSasha;
            State = HumanState.EmoteAction;
            Cache.Animation.CrossFade(animation, 0.1f);
            _stateTimeLeft = Cache.Animation[animation].length;
        }

        protected void Update()
        {
            if (State != HumanState.Idle)
            {
                _stateTimeLeft -= Time.deltaTime;
                if (_stateTimeLeft <= 0f)
                {
                    Idle();
                }
            }
        }

        protected void FixedUpdate()
        {
        }
    }
}
