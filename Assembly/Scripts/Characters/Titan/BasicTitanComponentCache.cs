﻿using UnityEngine;
using Utility;
using ApplicationManagers;

namespace Characters
{
    class BasicTitanComponentCache: BaseTitanComponentCache
    {
        public BaseHitbox BodyHitbox;
        public Transform ForearmL;
        public Transform ForearmR;
        public Collider ForearmLHurtbox;
        public Collider ForearmRHurtbox;
        public ParticleSystem ForearmBloodL;
        public ParticleSystem ForearmBloodR;
        public ParticleSystem ForearmSmokeL;
        public ParticleSystem ForearmSmokeR;
        public ParticleSystem NapeBlood;

        public BasicTitanComponentCache(GameObject owner): base(owner)
        {
            Core = Transform.Find("Amarture_VER2/Core");
            Neck = Transform.Find("Amarture_VER2/Core/Controller.Body/hip/spine/chest/neck");
            Hip = Transform.Find("Amarture_VER2/Core/Controller.Body/hip");
            Head = Neck.Find("head");
            GrabLSocket = Transform.Find("Amarture_VER2/Core/Controller.Body/hip/spine/chest/shoulder.L/upper_arm.L/forearm.L/hand.L/GrabLSocket");
            GrabRSocket = Transform.Find("Amarture_VER2/Core/Controller.Body/hip/spine/chest/shoulder.R/upper_arm.R/forearm.R/hand.R/GrabRSocket");
            BaseCharacter character = owner.GetComponent<BaseCharacter>();
            foreach (Collider collider in owner.GetComponentsInChildren<Collider>())
            {
                string name = collider.gameObject.name;
                if (name == "ForearmLHurtbox")
                    ForearmLHurtbox = collider;
                else if (name == "ForearmRHurtbox")
                    ForearmRHurtbox = collider;
                else if (name == "BodyHitbox")
                {
                    BodyHitbox = BaseHitbox.Create(character, collider.gameObject, collider);
                    Hitboxes.Add(BodyHitbox);
                }
            }
            SetupParticles();
            LoadAudio("TitanSounds", Neck);
        }

        private void SetupParticles()
        {
            ForearmL = Transform.Find("Amarture_VER2/Core/Controller.Body/hip/spine/chest/shoulder.L/upper_arm.L/forearm.L");
            ForearmR = Transform.Find("Amarture_VER2/Core/Controller.Body/hip/spine/chest/shoulder.R/upper_arm.R/forearm.R");
            ForearmBloodL = AssetBundleManager.InstantiateAsset<GameObject>("ArmBloodParticle", true).GetComponent<ParticleSystem>();
            ForearmBloodR = AssetBundleManager.InstantiateAsset<GameObject>("ArmBloodParticle", true).GetComponent<ParticleSystem>();
            ForearmBloodL.transform.SetParent(ForearmL);
            ForearmBloodL.transform.localPosition = Vector3.zero;
            ForearmBloodL.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ForearmBloodL.transform.localScale = Vector3.one * 0.01f;
            ForearmBloodR.transform.SetParent(ForearmR);
            ForearmBloodR.transform.localPosition = Vector3.zero;
            ForearmBloodR.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ForearmBloodR.transform.localScale = Vector3.one * 0.01f;
            ForearmSmokeL = AssetBundleManager.InstantiateAsset<GameObject>("ArmSmokeParticle", true).GetComponent<ParticleSystem>();
            ForearmSmokeR = AssetBundleManager.InstantiateAsset<GameObject>("ArmSmokeParticle", true).GetComponent<ParticleSystem>();
            ForearmSmokeL.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ForearmSmokeR.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            NapeBlood = AssetBundleManager.InstantiateAsset<GameObject>("NapeBloodParticle", true).GetComponent<ParticleSystem>();
            NapeBlood.transform.SetParent(Neck);
            NapeBlood.transform.localPosition = new Vector3(0f, 0.02f, -0.1f);
            NapeBlood.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            NapeBlood.transform.localScale = Vector3.one * 0.01f;
        }
    }
}
