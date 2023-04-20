using System.Collections;
using UnityEngine;

namespace Characters
{
    class PotatoSpecial : BaseEmoteSpecial
    {
        protected override float ActiveTime => 10f;
        private float _oldSpeed;

        public PotatoSpecial(BaseCharacter owner): base(owner)
        {
            Cooldown = 30f;
        }

        protected override void Activate()
        {
            _oldSpeed = _human.RunSpeed;
            _human.RunSpeed = _oldSpeed * 4f;
            _human.RunAnimation = HumanAnimations.RunBuffed;
            _human.EmoteAnimation(HumanAnimations.SpecialSasha);
        }

        protected override void Deactivate()
        {
            _human.RunSpeed = _oldSpeed;
            _human.RunAnimation = HumanAnimations.Run;
        }
    }
}
