using System.Collections;
using UnityEngine;

namespace Characters
{
    class ShifterTransformSpecial : SimpleUseable
    {
        protected float LiveTime = 60f;
        protected string _shifter;

        public ShifterTransformSpecial(BaseCharacter owner, string shifter): base(owner)
        {
            Cooldown = 60f;
            _shifter = shifter;
            SetCooldownLeft(Cooldown);
        }

        protected override void Activate()
        {
            var human = (Human)_owner;
            human.TransformShifter(_shifter, LiveTime);
        }
    }
}
