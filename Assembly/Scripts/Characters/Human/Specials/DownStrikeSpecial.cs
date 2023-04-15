using System.Collections;
using UnityEngine;

namespace Characters
{
    class DownStrikeSpecial : BaseAttackSpecial
    {
        protected override float ActiveTime => 0.7f;
        protected bool _needActivate;

        public DownStrikeSpecial(BaseCharacter owner): base(owner)
        {
            Cooldown = 10f;
        }

        protected override void Activate()
        {
            _needActivate = true;
            _human.StartSpecialAttack(HumanAnimations.SpecialMikasa1);
        }

        protected override void ActiveFixedUpdate()
        {
            base.ActiveFixedUpdate();
            if (_needActivate && _activeTimeLeft < 0.4f)
            {
                _needActivate = false;
                _human.ActivateBlades();
                _human.PlaySound(HumanSounds.BladeSwing);
                _human.Cache.Rigidbody.AddForce(Vector3.down * 30f, ForceMode.VelocityChange);
            }
        }
    }
}
