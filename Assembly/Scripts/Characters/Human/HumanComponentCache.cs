using UnityEngine;
using Utility;
using ApplicationManagers;
using Xft;

namespace Characters
{
    class HumanComponentCache: BaseComponentCache
    {
        public Transform HandL;
        public Transform HandR;
        public Transform Head;
        public Transform Neck;
        public Transform ForearmL;
        public Transform ForearmR;
        public Transform UpperarmL;
        public Transform UpperarmR;
        public ParticleSystem Sparks;
        public ParticleSystem Smoke;
        public ParticleSystem Wind;
        public Transform WindTransform;
        public Transform HookLeftAnchorDefault;
        public Transform HookRightAnchorDefault;
        public Transform HookLeftAnchorGun;
        public Transform HookRightAnchorGun;
        public BaseHitbox BladeHitLeft;
        public BaseHitbox BladeHitRight;
        public BaseHitbox AHSSHit;
        public BaseHitbox APGHit;
        
        public HumanComponentCache(GameObject owner): base(owner)
        {
            Head = Transform.Find("Amarture/Controller_Body/hip/spine/chest/neck/head");
            Neck = Transform.Find("Amarture/Controller_Body/hip/spine/chest/neck");
            HandL = Transform.Find("Amarture/Controller_Body/hip/spine/chest/shoulder_L/upper_arm_L/forearm_L/hand_L");
            HandR = Transform.Find("Amarture/Controller_Body/hip/spine/chest/shoulder_R/upper_arm_R/forearm_R/hand_R");
            ForearmL = Transform.Find("Amarture/Controller_Body/hip/spine/chest/shoulder_L/upper_arm_L/forearm_L");
            ForearmR = Transform.Find("Amarture/Controller_Body/hip/spine/chest/shoulder_R/upper_arm_R/forearm_R");
            UpperarmL = Transform.Find("Amarture/Controller_Body/hip/spine/chest/shoulder_L/upper_arm_L");
            UpperarmR = Transform.Find("Amarture/Controller_Body/hip/spine/chest/shoulder_R/upper_arm_R");
            Sparks = Transform.Find("slideSparks").GetComponent<ParticleSystem>();
            Smoke = Transform.Find("3dmg_smoke").GetComponent<ParticleSystem>();
            Smoke.enableEmission = false;
            Sparks.enableEmission = false;
            Wind = Transform.Find("speedFX").GetComponentInChildren<ParticleSystem>();
            WindTransform = Transform.Find("speedFX");
            Wind.enableEmission = false;
            HERO hero = owner.GetComponent<HERO>();
            HookLeftAnchorDefault = hero.hookRefL1.transform;
            HookRightAnchorDefault = hero.hookRefR1.transform;
            HookLeftAnchorGun = hero.hookRefL2.transform;
            HookRightAnchorGun = hero.hookRefR2.transform;
            var human = owner.GetComponent<BaseCharacter>();
            if (human != null)
            {
                BladeHitLeft = BaseHitbox.Create(human, hero.checkBoxLeft);
                BladeHitLeft.OnEnter = false;
                BladeHitRight = BaseHitbox.Create(human, hero.checkBoxRight);
                BladeHitRight.OnEnter = false;
                CreateAHSSHitbox(human);
                CreateAPGHitbox(human);
                LoadAudio("HumanSounds", Transform);
            }
        }

        private void CreateAHSSHitbox(BaseCharacter human)
        {
            GameObject obj = new GameObject();
            obj.layer = PhysicsLayer.Hitbox;
            var capsule = obj.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.isTrigger = true;
            var ahssInfo = CharacterData.HumanWeaponInfo["AHSS"];
            capsule.radius = ahssInfo["Radius"].AsFloat;
            capsule.height = ahssInfo["Range"].AsFloat;
            capsule.center = new Vector3(0f, 0f, capsule.height * 0.5f + 0.5f);
            AHSSHit = BaseHitbox.Create(human, obj);
        }

        private void CreateAPGHitbox(BaseCharacter human)
        {
            GameObject obj = new GameObject();
            obj.layer = PhysicsLayer.Hitbox;
            var capsule = obj.AddComponent<CapsuleCollider>();
            capsule.direction = 2;
            capsule.isTrigger = true;
            var ahssInfo = CharacterData.HumanWeaponInfo["APG"];
            capsule.radius = ahssInfo["Radius1"].AsFloat;
            capsule.height = ahssInfo["Range"].AsFloat;
            capsule.center = new Vector3(0f, 0f, capsule.height * 0.5f + 0.5f);
            APGHit = BaseHitbox.Create(human, obj);
        }
    }
}
