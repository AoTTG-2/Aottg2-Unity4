﻿using System;
using UnityEngine;
using ApplicationManagers;
using GameManagers;
using UnityEngine.UI;
using Utility;
using Controllers;
using CustomSkins;
using System.Collections.Generic;
using SimpleJSONFixed;
using Effects;

namespace Characters
{
    class AnnieShifter: BaseShifter
    {
        protected AnnieComponentCache AnnieCache;
        protected AnnieAnimations AnnieAnimations;
        protected AnnieCustomSkinLoader _customSkinLoader;
        public override List<string> EmoteActions => new List<string>() { "Salute", "Roar", "Taunt", "Wave" };

        protected override void CreateCache(BaseComponentCache cache)
        {
            AnnieCache = new AnnieComponentCache(gameObject);
            base.CreateCache(AnnieCache);
        }

        protected override void CreateAnimations(BaseTitanAnimations animations)
        {
            AnnieAnimations = new AnnieAnimations();
            base.CreateAnimations(AnnieAnimations);
        }

        public override void Emote(string emote)
        {
            string anim = string.Empty;
            if (emote == "Salute")
                anim = AnnieAnimations.EmoteSalute;
            else if (emote == "Roar")
            {
                anim = AnnieAnimations.EmoteRoar;
                StartCoroutine(WaitAndPlaySound(ShifterSounds.AnnieRoar, 0.1f));
            }
            else if (emote == "Taunt")
                anim = AnnieAnimations.EmoteTaunt;
            else if (emote == "Wave")
                anim = AnnieAnimations.EmoteWave;
            if (anim != "")
                StateAction(TitanState.Emote, anim);
        }

        protected override void Awake()
        {
            base.Awake();
            _customSkinLoader = gameObject.AddComponent<AnnieCustomSkinLoader>();
        }

        protected override Dictionary<string, float> GetRootMotionAnimations()
        {
            return new Dictionary<string, float>() { { AnnieAnimations.Attack, 0.95f } };
        }

        public override void Attack(string attack)
        {
            ResetAttackState(attack);
            if (_currentAttack == ShifterAttacks.AttackDefault)
                StateAction(TitanState.Attack, AnnieAnimations.Attack);
            else if (_currentAttack == ShifterAttacks.AttackKick)
                StateAction(TitanState.Attack, AnnieAnimations.Kick);
            else if (_currentAttack == AnnieAttacks.AttackSwing)
                StateAction(TitanState.Attack, AnnieAnimations.AttackSwing);
            else if (_currentAttack == AnnieAttacks.AttackBrush)
            {
                DeactivateAllHitboxes();
                string animation = AttackBrush();
                StateAction(TitanState.Attack, animation, deactivateHitboxes: false);
            }
        }

        protected string AttackBrush()
        {
            float[] angles = GetNearestHumanAngles();
            float angleX = angles[0];
            float distanceY;
            float distanceZ;
            if (TargetEnemy == null)
            {
                BaseTitanCache.HandRHitbox.Activate(0.96f, 0.13f);
                return AnnieAnimations.AttackBrushBack;
            }
            else
            {
                Vector3 diff = Cache.Transform.InverseTransformPoint(TargetEnemy.Cache.Transform.position);
                distanceY = diff.y;
                distanceZ = diff.z;
            }
            if (distanceZ < 0f)
            {
                if (distanceY < 35f * Size)
                {
                    BaseTitanCache.HandRHitbox.Activate(0.2f, 0.34f);
                    return AnnieAnimations.AttackBrushBack;
                }
                else
                {
                    if (angleX < 0f)
                    {
                        BaseTitanCache.HandLHitbox.Activate(0.35f, 0.237f);
                        return AnnieAnimations.AttackBrushHeadL;
                    }
                    else
                    {
                        BaseTitanCache.HandRHitbox.Activate(0.35f, 0.237f);
                        return AnnieAnimations.AttackBrushHeadR;
                    }
                }
            }
            else
            {
                if (angleX < 0f)
                {
                    BaseTitanCache.HandRHitbox.Activate(0.288f, 0.373f);
                    return AnnieAnimations.AttackBrushFrontL;
                }
                else
                {
                    BaseTitanCache.HandLHitbox.Activate(0.288f, 0.373f);
                    return AnnieAnimations.AttackBrushFrontR;
                }
            }
        }

        protected override void UpdateAttack()
        {
            float animationTime = GetAnimationTime();
            if (_currentStateAnimation == AnnieAnimations.Attack)
            {
                if(_currentAttackStage == 0 && animationTime > 0.16f)
                {
                    _currentAttackStage = 1;
                    AnnieCache.FootRHitbox.Activate(0f, 0.05f);
                    PlaySound(TitanSounds.Swing1);
                }
                else if (_currentAttackStage ==  1 && animationTime > 0.31f)
                {
                    _currentAttackStage = 2;
                    AnnieCache.FootLHitbox.Activate(0f, 0.11f);
                    PlaySound(TitanSounds.Swing2);
                }
                else if (_currentAttackStage == 2 && animationTime > 0.59f)
                {
                    _currentAttackStage = 3;
                    AnnieCache.FootRHitbox.Activate(0f, 0.13f);
                    PlaySound(TitanSounds.Swing3);
                }
            }
            else if (_currentStateAnimation == AnnieAnimations.Kick)
            {
                if (_currentAttackStage == 0 && animationTime > 0.38f)
                {
                    _currentAttackStage = 1;
                    AnnieCache.FootRHitbox.Activate(0f, 0.13f);
                    PlaySound(TitanSounds.Swing1);
                }
            }
            else if (_currentStateAnimation == AnnieAnimations.AttackSwing)
            {
                if (_currentAttackStage == 0 && animationTime > 0.45f)
                {
                    _currentAttackStage = 1;
                    AnnieCache.HandRHitbox.Activate(0f, 0.12f);
                    PlaySound(TitanSounds.Swing1);
                }
            }
        }
    }
}
