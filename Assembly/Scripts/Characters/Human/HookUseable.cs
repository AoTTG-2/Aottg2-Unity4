using ApplicationManagers;
using UnityEngine;
using Utility;
using System.Collections.Generic;

namespace Characters
{
    class HookUseable : HoldUseable
    {
        public List<Hook> Hooks = new List<Hook>();
        private Hook _activeHook = null;

        public HookUseable(BaseCharacter owner, bool left, bool gun) : base(owner)
        {
            for (int i = 0; i < 3; i++)
                Hooks.Add(Hook.CreateHook((Human)owner, left, i, gun));
        }

        public bool IsHooked()
        {
            return _activeHook != null && _activeHook.State == HookState.Hooked;
        }

        public bool HasHook()
        {
            return _activeHook != null && _activeHook.State != HookState.Disabled;
        }

        public Vector3 GetHookPosition()
        {
            if (_activeHook != null)
                return _activeHook.GetHookPosition();
            Debug.Log("Warning: zero hook position");
            return Vector3.zero;
        }

        public Transform GetHookParent()
        {
            return _activeHook.HookParent;
        }

        public BaseCharacter GetHookCharacter()
        {
            return _activeHook.HookCharacter;
        }

        public void DisableActiveHook()
        {
            if (_activeHook != null && _activeHook.State == HookState.Hooked)
                _activeHook.SetHookState(HookState.DisablingHooked);
            _activeHook = null;
        }

        public void DisableAnyHook()
        {
            if (_activeHook != null)
            {
                if (_activeHook.State == HookState.Hooked)
                    _activeHook.SetHookState(HookState.DisablingHooked);
                else if (_activeHook.State == HookState.Hooking)
                    _activeHook.SetHookState(HookState.DisablingHooking);
            }
            _activeHook = null;
        }

        protected override void Activate()
        {
            StartHook();
        }

        protected override void ActiveFixedUpdate()
        {
            StartHook();
        }

        private void StartHook()
        {
            if (_activeHook != null && _activeHook.State == HookState.Disabled)
                _activeHook = null;
            if (_activeHook == null)
            {
                _activeHook = FindAvailableHook();
                Vector3 target = ((Human)_owner).GetAimPoint();
                Vector3 baseVel = (target - _activeHook.Anchor.position).normalized * 3f;
                Vector3 playerVel = _owner.Cache.Rigidbody.velocity;
                Vector3 relativeVel = Vector3.zero;
                float f = Mathf.Acos(Vector3.Dot(baseVel.normalized, playerVel.normalized)) * Mathf.Rad2Deg;
                if (Mathf.Abs(f) <= 90f)
                    relativeVel = Vector3.Project(playerVel, baseVel);
                _activeHook.SetHooking(baseVel, relativeVel);
            }
        }

        protected override void Deactivate()
        {
        }

        private Hook FindAvailableHook()
        {
            foreach (Hook hook in Hooks)
            {
                if (hook.State == HookState.Disabled)
                    return hook;
            }
            foreach (Hook hook in Hooks)
            {
                if (hook.State == HookState.DisablingHooked || hook.State == HookState.DisablingHooking)
                    return hook;
            }
            return Hooks[0];
        }
    }
}
