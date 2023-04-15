using System.Collections;
using UnityEngine;

namespace Characters
{
    class Spin2Special : BaseAttackSpecial
    {
        protected override float ActiveTime => 0.74f;
        protected float AnimationLoopStartTime = 0.35f;
        protected float AnimationLoopEndTime = 0.48f;
        protected int Loops = 3;
        protected int _stage;

        public Spin2Special(BaseCharacter owner): base(owner)
        {
            Cooldown = 10f;
        }

        protected override void Activate()
        {
            _stage = 0;
            _human.StartSpecialAttack(HumanAnimations.SpecialPetra);
        }

        protected override void ActiveFixedUpdate()
        {
            base.ActiveFixedUpdate();
            if (!_human.Cache.Animation.IsPlaying(HumanAnimations.SpecialPetra))
                return;
            float time = GetAnimationTime();
            if (_stage == 0 && time > AnimationLoopStartTime)
            {
                _human.ActivateBlades();
                _human.Cache.Rigidbody.AddForce(Vector3.up * 30f, ForceMode.VelocityChange);
                _human.PlaySound(HumanSounds.BladeSwing);
                _stage += 1;
            }
            else if (_stage < Loops && time > AnimationLoopEndTime)
            {
                _human.PlayAnimation(HumanAnimations.SpecialPetra, AnimationLoopStartTime);
                _human.PlaySound(HumanSounds.BladeSwing);
                _stage += 1;
            }
        }

        protected float GetAnimationTime()
        {
            return _human.Cache.Animation[HumanAnimations.SpecialPetra].normalizedTime;
        }
    }
}
