using System;
using UnityEngine;
using ApplicationManagers;
using GameManagers;
using UnityEngine.UI;
using Settings;
using Characters;
using UI;
using System.Collections.Generic;
using Utility;
using CustomLogic;

namespace Controllers
{
    class HumanPlayerController : BasePlayerController
    {
        protected Human _human;
        protected float _reelOutScrollTimeLeft;
        protected HumanInputSettings _humanInput;
        protected Dictionary<HumanDashDirection, KeybindSetting> _dashKeys;
        protected Dictionary<HumanDashDirection, float> _dashTimes;
        protected static LayerMask HookMask = PhysicsLayer.GetMask(PhysicsLayer.TitanMovebox, PhysicsLayer.TitanPushbox,
            PhysicsLayer.MapObjectEntities, PhysicsLayer.MapObjectProjectiles, PhysicsLayer.MapObjectAll);

        protected override void Awake()
        {
            base.Awake();
            _human = GetComponent<Human>();
            _humanInput = SettingsManager.InputSettings.Human;
            SetupDash();
        }

        private void SetupDash()
        {
            GeneralInputSettings general = SettingsManager.InputSettings.General;
            _dashKeys = new Dictionary<HumanDashDirection, KeybindSetting>() {
                { HumanDashDirection.Forward, general.Forward },
                { HumanDashDirection.Back, general.Back },
                { HumanDashDirection.Left, general.Left },
                { HumanDashDirection.Right, general.Right }
            };
            _dashTimes = new Dictionary<HumanDashDirection, float>()
            {
                { HumanDashDirection.Forward, -1f },
                { HumanDashDirection.Back, -1f },
                { HumanDashDirection.Left, -1f },
                { HumanDashDirection.Right, -1f }
            };
        }

        protected override void Update()
        {
            if (!_human.FinishSetup)
                return;
            base.Update();
        }

        protected override void UpdateMovementInput(bool inMenu)
        {
            if (inMenu || _human.Dead)
            {
                _human.HasDirection = false;
                return;
            }
            _human.IsWalk = _humanInput.HorseWalk.GetKey();
            if (_human.MountState != HumanMountState.Horse)
            {
                if (_human.Grounded && _human.State != HumanState.Idle)
                    return;
                if (!_human.Grounded && (_human.State == HumanState.EmoteAction || _human.State == HumanState.SpecialAttack ||
                    _human.Cache.Animation.IsPlaying("dash") || _human.Cache.Animation.IsPlaying("jump") || _human.IsFiringThunderspear()))
                    return;
            }
            int forward = 0;
            int right = 0;
            if (_generalInput.Forward.GetKey())
                forward = 1;
            else if (_generalInput.Back.GetKey())
                forward = -1;
            if (_generalInput.Left.GetKey())
                right = -1;
            else if (_generalInput.Right.GetKey())
                right = 1;
            if (forward != 0 || right != 0)
            {
                _character.TargetAngle = GetTargetAngle(forward, right);
                _character.HasDirection = true;
                Vector3 v = new Vector3(right, 0f, forward);
                float magnitude = (v.magnitude <= 0.95f) ? ((v.magnitude >= 0.25f) ? v.magnitude : 0f) : 1f;
                _human.TargetMagnitude = magnitude;
            }
            else
                _character.HasDirection = false;
        }

        protected override void UpdateUI(bool inMenu)
        {
            Ray ray = SceneLoader.CurrentCamera.Camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            string str = string.Empty;
            string distance = "???";
            float magnitude = 1000f;
            float speed = _human.Cache.Rigidbody.velocity.magnitude;
            if (Physics.Raycast(ray, out hit, 1000f, HookMask.value))
            {
                magnitude = (hit.point - _human.Cache.Transform.position).magnitude;
                distance = ((int)magnitude).ToString();
            }
            if (SettingsManager.UISettings.ShowCrosshairDistance.Value)
                str += distance;
            if (SettingsManager.UISettings.Speedometer.Value == (int)SpeedometerType.Speed)
            {
                if (str != string.Empty)
                    str += "\n";
                str += speed.ToString("F1") + " u/s";
            }
            else if (SettingsManager.UISettings.Speedometer.Value == (int)SpeedometerType.Damage)
            {
                if (str != string.Empty)
                    str += "\n";
                str += ((speed / 100f)).ToString("F1") + "K";
            }
            CursorManager.SetCrosshairText(str);
            if (magnitude > 120f)
            {
                CursorManager.SetCrosshairColor(false);
            }
            else
            {
                CursorManager.SetCrosshairColor(true);
            }
        }

        void UpdateHookInput(bool inMenu)
        {
            bool canHook = _human.State != HumanState.Grab  && _human.State != HumanState.SpecialAttack && _human.CurrentGas > 0f && !inMenu && _human.MountState != HumanMountState.MapObject && !_human.Dead;
            _human.HookLeft.SetInput(canHook && _humanInput.HookLeft.GetKey());
            _human.HookRight.SetInput(canHook && _humanInput.HookRight.GetKey());
        }

        protected override void UpdateActionInput(bool inMenu)
        {
            UpdateHookInput(inMenu);
            UpdateReelInput(inMenu);
            UpdateDashInput(inMenu);
            var states = new HashSet<HumanState>() { HumanState.Grab, HumanState.SpecialAction, HumanState.EmoteAction, HumanState.Reload,
            HumanState.SpecialAttack};
            bool canWeapon = _human.MountState == HumanMountState.None && !states.Contains(_human.State) && !inMenu && !_human.Dead;
            if (canWeapon)
            {
                if (_human.Weapon is AmmoWeapon && ((AmmoWeapon)_human.Weapon).RoundLeft == 0)
                {
                    if (_humanInput.AttackDefault.GetKeyDown() && _human.State == HumanState.Idle)
                        _human.Reload();
                }
                else
                    _human.Weapon.ReadInput(_humanInput.AttackDefault);
            }
            else
                _human.Weapon.SetInput(false);
            if (_human.Special != null)
            {
                bool canSpecial = _human.MountState == HumanMountState.None && 
                    (_human.Special is EscapeSpecial || _human.Special is ShifterTransformSpecial || _human.State != HumanState.Grab)
                    && _human.State != HumanState.EmoteAction && _human.State != HumanState.SpecialAttack && !inMenu && !_human.Dead;
                if (canSpecial)
                    _human.Special.ReadInput(_humanInput.AttackSpecial);
                else
                    _human.Special.SetInput(false);
            }
            if (inMenu || _human.Dead)
                return;
            if (_human.MountState == HumanMountState.None)
            {
                if (_human.CanJump())
                {
                    if (_humanInput.Jump.GetKeyDown())
                        _human.Jump();
                    else if (_humanInput.HorseMount.GetKeyDown() && _human.Horse != null && _human.MountState == HumanMountState.None &&
                    Vector3.Distance(_human.Horse.Cache.Transform.position, _human.Cache.Transform.position) < 15f)
                        _human.MountHorse();
                    else if (_humanInput.Dodge.GetKeyDown())
                    {
                        if (_human.HasDirection)
                            _human.Dodge(_human.TargetAngle + 180f);
                        else
                            _human.Dodge(_human.TargetAngle);
                    }
                }
                if (_human.State == HumanState.Idle)
                {
                    if (_humanInput.Reload.GetKeyDown())
                        _human.Reload();
                }
            }
            else if (_human.MountState == HumanMountState.Horse)
            {
                if (_humanInput.HorseMount.GetKeyDown())
                    _human.Unmount(false);
            }
        }

        void UpdateReelInput(bool inMenu)
        {
            _reelOutScrollTimeLeft -= Time.deltaTime;
            if (_reelOutScrollTimeLeft <= 0f)
                _human.ReelOutAxis = 0f;
            if (_humanInput.ReelIn.GetKey())
                _human.ReelInAxis = -1f;
            foreach (InputKey inputKey in _humanInput.ReelOut.InputKeys)
            {
                if (inputKey.GetKey())
                {
                    _human.ReelOutAxis = 1f;
                    if (inputKey.IsWheel())
                        _reelOutScrollTimeLeft = SettingsManager.InputSettings.Human.ReelOutScrollSmoothing.Value;
                }
            }
        }

        void UpdateDashInput(bool inMenu)
        {
            if (!_human.Grounded && (_human.State != HumanState.AirDodge) && _human.MountState == HumanMountState.None && _human.State != HumanState.Grab
                && _human.State != HumanState.EmoteAction && _human.State != HumanState.SpecialAttack && _human.State != HumanState.SpecialAction
                && !inMenu && !_human.Dead)
            {
                HumanDashDirection currentDirection = HumanDashDirection.None;
                if (_humanInput.Dash.GetKeyDown())
                {
                    foreach (HumanDashDirection direction in _dashKeys.Keys)
                    {
                        if (_dashKeys[direction].GetKey())
                        {
                            currentDirection = direction;
                            break;
                        }
                    }
                }
                if (SettingsManager.InputSettings.Human.DashDoubleTap.Value)
                {
                    foreach (HumanDashDirection direction in _dashKeys.Keys)
                    {
                        if (_dashTimes[direction] >= 0f)
                        {
                            _dashTimes[direction] += Time.deltaTime;
                            if (_dashTimes[direction] > 0.2f)
                                _dashTimes[direction] = -1f;
                        }
                        if (_dashKeys[direction].GetKeyDown())
                        {
                            if (_dashTimes[direction] == -1f)
                                _dashTimes[direction] = 0f;
                            else if (_dashTimes[direction] > 0f)
                                currentDirection = direction;
                        }
                    }
                }
                if (currentDirection != HumanDashDirection.None)
                    _human.Dash(GetDashAngle(currentDirection));
            }
        }

        float GetDashAngle(HumanDashDirection direction)
        {
            float angle = 0f;
            if (direction == HumanDashDirection.Forward)
                angle = GetTargetAngle(1, 0);
            else if (direction == HumanDashDirection.Back)
                angle = GetTargetAngle(-1, 0);
            else if (direction == HumanDashDirection.Right)
                angle = GetTargetAngle(0, 1);
            else if (direction == HumanDashDirection.Left)
                angle = GetTargetAngle(0, -1);
            return angle;
        }
    }
}
