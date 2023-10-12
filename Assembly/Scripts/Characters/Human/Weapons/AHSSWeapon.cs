using Effects;
using GameManagers;
using UI;
using UnityEngine;
using Utility;


namespace Characters
{
    class AHSSWeapon : AmmoWeapon
    {
        public AHSSWeapon(BaseCharacter owner, int ammo, int ammoPerRound, float cooldown) : base(owner, ammo, ammoPerRound, cooldown)
        {
        }

        protected override void Activate()
        {
            var human = (Human)_owner;
            string anim = "";
            bool left = !human.HookLeft.IsHooked();
            if (human.Grounded)
            {
                if (left)
                    anim = "AHSS_shoot_l";
                else
                    anim = "AHSS_shoot_r";
            }
            else
            {
                if (left)
                    anim = "AHSS_shoot_l_air";
                else
                    anim = "AHSS_shoot_r_air";
            }
            human.State = HumanState.Attack;
            human.AttackAnimation = anim;
            human.CrossFade(anim, 0.05f);
            Vector3 target = human.GetAimPoint();
            Vector3 direction = (target - human.Cache.Transform.position).normalized;
            human.TargetAngle = Quaternion.LookRotation(direction).eulerAngles.y;
            human._targetRotation = Quaternion.Euler(0f, human.TargetAngle, 0f);
            human.Cache.Transform.rotation = Quaternion.Lerp(human.Cache.Transform.rotation, human._targetRotation, Time.deltaTime * 30f);
            Vector3 start = human.Cache.Transform.position + human.Cache.Transform.up * 0.8f;
            direction = (target - start).normalized;
            EffectSpawner.Spawn(EffectPrefabs.GunExplode, start, Quaternion.LookRotation(direction));
            human.HumanCache.AHSSHit.transform.position = start;
            human.HumanCache.AHSSHit.transform.rotation = Quaternion.LookRotation(direction);
            human.HumanCache.AHSSHit.Activate(0f, 0.1f);
            ((InGameMenu)UIManager.CurrentMenu).HUDBottomHandler.ShootGun();
        }
    }
}
