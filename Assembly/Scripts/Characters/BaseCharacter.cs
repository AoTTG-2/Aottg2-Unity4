using System;
using UnityEngine;
using ApplicationManagers;
using GameManagers;
using UnityEngine.UI;
using Utility;
using System.Collections.Generic;
using Settings;
using System.Collections;
using CustomLogic;
using UI;
using Cameras;

namespace Characters
{
    class BaseCharacter: Photon.MonoBehaviour
    {
        protected virtual int DefaultMaxHealth => 1;
        protected virtual bool HasMovement => true;
        protected virtual Vector3 Gravity => Vector3.down * 20f;
        public virtual List<string> EmoteActions => new List<string>();
        public string Name = "";
        public string Guild = "";
        public bool Dead;
        public bool CustomDamageEnabled;
        public int CustomDamage;

        // setup
        public BaseComponentCache Cache;
        public bool AI;
        public int MaxHealth;
        public int CurrentHealth;
        public string Team;
        public List<BaseUseable> Items = new List<BaseUseable>();
        protected InGameManager _inGameManager;
        protected BaseMovementSync _movementSync;

        // movement
        public bool Grounded;
        public bool JustGrounded;
        public float TargetAngle;
        public bool HasDirection;
        protected int _stepPhase = 0;
        public virtual LayerMask GroundMask => PhysicsLayer.GetMask(PhysicsLayer.TitanMovebox, PhysicsLayer.MapObjectEntities,
                PhysicsLayer.MapObjectCharacters, PhysicsLayer.MapObjectAll);
        protected virtual float GroundDistance => 0.3f;

        public bool IsMine()
        {
            return Cache.PhotonView.isMine;
        }

        public bool IsMainCharacter()
        {
            return _inGameManager.CurrentCharacter == this;
        }

        public virtual void Init(bool ai, string team)
        {
            AI = ai;
            if (!ai)
            {
                Name = PhotonNetwork.player.GetStringProperty(PlayerProperty.Name);
                Guild = PhotonNetwork.player.GetStringProperty(PlayerProperty.Guild);
            }
            Cache.PhotonView.RPC("InitRPC", PhotonTargets.AllBuffered, new object[] { AI, Name, Guild });
            SetTeam(team);
        }

        public void SetTeam(string team)
        {
            Cache.PhotonView.RPC("SetTeamRPC", PhotonTargets.All, new object[] { team });
        }

        public virtual Transform GetCameraAnchor()
        {
            return Cache.Transform;
        }

        protected virtual void CreateCache(BaseComponentCache cache)
        {
            Cache = cache;
            if (cache == null)
                Cache = new BaseComponentCache(gameObject);
        }

        public virtual void Emote(string emote)
        {
        }

        [RPC]
        public void InitRPC(bool ai, string name, string guild)
        {
            AI = ai;
            Name = name;
            Guild = guild;
            if (HasMovement)
                _movementSync = CreateMovementSync();
        }

        [RPC]
        public void SetHealthRPC(int currentHealth, int maxHealth, PhotonMessageInfo info)
        {
            if (info.sender == photonView.owner)
            {
                CurrentHealth = currentHealth;
                MaxHealth = maxHealth;
            }
        }

        [RPC]
        public void SetTeamRPC(string team, PhotonMessageInfo info)
        {
            if (info.sender == photonView.owner)
            {
                Team = team;
            }
        }

        public void SetCurrentHealth(int currentHealth)
        {
            CurrentHealth = Mathf.Min(currentHealth, MaxHealth);
            CurrentHealth = Mathf.Max(CurrentHealth, 0);
            OnHealthChange();
            if (CurrentHealth <= 0)
                Die();
        }

        public void SetMaxHealth(int maxHealth)
        {
            MaxHealth = maxHealth;
            SetCurrentHealth(CurrentHealth);
        }

        public void SetHealth(int health)
        {
            MaxHealth = health;
            SetCurrentHealth(health);
        }

        public virtual void TakeDamage(int damage)
        {
            SetCurrentHealth(CurrentHealth - damage);
        }

        public virtual void Die()
        {
            Cache.PhotonView.RPC("MarkDeadRPC", PhotonTargets.AllBuffered, new object[0]);
            if (IsMainCharacter())
                _inGameManager.RegisterMainCharacterDie();
            StartCoroutine(WaitAndDie());
        }

        protected virtual IEnumerator WaitAndDie()
        {
            PhotonNetwork.Destroy(gameObject);
            yield break;
        }

        public virtual void UseItem(int item)
        {
            Items[item].SetInput(true);
        }

        public virtual void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            if (Cache.PhotonView.isMine)
            {
                Cache.PhotonView.RPC("SetHealthRPC", player, new object[] { CurrentHealth, MaxHealth });
                Cache.PhotonView.RPC("SetTeamRPC", player, new object[] { Team });
                string currentAnimation = GetCurrentAnimation();
                if (currentAnimation != "")
                    Cache.PhotonView.RPC("PlayAnimationRPC", player, new object[] { currentAnimation, Cache.Animation[currentAnimation].normalizedTime });
            }
        }

        public void PlayAnimation(string animation, float startTime = 0f)
        {

            Cache.PhotonView.RPC("PlayAnimationRPC", PhotonTargets.All, new object[] { animation, startTime });
        }

        public void PlayAnimationReset(string animation)
        {

            Cache.PhotonView.RPC("PlayAnimationResetRPC", PhotonTargets.All, new object[] { animation });
        }

        [RPC]
        public void PlayAnimationRPC(string animation, float startTime, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            Cache.Animation.Play(animation);
            if (startTime > 0f)
                Cache.Animation[animation].normalizedTime = startTime;
        }

        [RPC]
        public void PlayAnimationResetRPC(string animation, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            Cache.Animation.Play(animation);
            Cache.Animation[animation].normalizedTime = 0f;
        }

        public void PlayAnimationIfNotPlaying(string animation, float startTime = 0f)
        {
            if (!Cache.Animation.IsPlaying(animation))
                PlayAnimation(animation, startTime);
        }

        public void CrossFade(string animation, float fadeTime = 0f, float startTime = 0f)
        {
            Cache.PhotonView.RPC("CrossFadeRPC", PhotonTargets.All, new object[] { animation, fadeTime, startTime });
        }

        public void CrossFadeIfNotPlaying(string animation, float fadeTime = 0f, float startTime = 0f)
        {
            if (!Cache.Animation.IsPlaying(animation))
                CrossFade(animation, fadeTime, startTime);
        }

        [RPC]
        public void CrossFadeRPC(string animation, float fadeTime, float startTime, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            Cache.Animation.CrossFade(animation, fadeTime);
            if (startTime > 0f)
                Cache.Animation[animation].normalizedTime = startTime;
        }

        public void PlaySound(string sound)
        {
            Cache.PhotonView.RPC("PlaySoundRPC", PhotonTargets.All, new object[] { sound });
        }

        protected IEnumerator WaitAndPlaySound(string sound, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlaySound(sound);
        }

        [RPC]
        public void PlaySoundRPC(string sound, PhotonMessageInfo info = null)
        {
            if (info != null && info.sender != Cache.PhotonView.owner)
                return;
            if (Cache.AudioSources.ContainsKey(sound))
                Cache.AudioSources[sound].Play();
        }

        public void StopSound(string sound)
        {
            Cache.PhotonView.RPC("StopSoundRPC", PhotonTargets.All, new object[] { sound });
        }

        [RPC]
        public void StopSoundRPC(string sound, PhotonMessageInfo info = null)
        {
            if (info != null && info.sender != Cache.PhotonView.owner)
                return;
            if (Cache.AudioSources.ContainsKey(sound))
                Cache.AudioSources[sound].Stop();
        }

        protected virtual void OnHealthChange()
        {
            if (IsMine())
                photonView.RPC("SetHealthRPC", PhotonTargets.All, new object[] { CurrentHealth, MaxHealth });
        }

        public virtual void OnHit(BaseHitbox hitbox, object victim, Collider collider, string type, bool firstHit)
        {
        }

        [RPC]
        public virtual void GetHitRPC(int viewId, string name, int damage, string type, string collider)
        {
            if (Dead)
                return;
            if (damage == 0)
                return;
            if (name == "")
            {
                var killer = Util.FindCharacterByViewId(viewId);
                if (killer != null)
                    name = killer.Name;
            }
            TakeDamage(damage);
            Cache.PhotonView.RPC("NotifyDamagedRPC", PhotonTargets.All, new object[] { viewId, name, damage });
            if (CurrentHealth <= 0f)
            {
                RPCManager.PhotonView.RPC("ShowKillFeedRPC", PhotonTargets.All, new object[] { name, Name, damage });
                Cache.PhotonView.RPC("NotifyDieRPC", PhotonTargets.All, new object[] { viewId, name });
            }
        }

        [RPC]
        public virtual void GetDamagedRPC(string name, int damage)
        {
            if (!Cache.PhotonView.isMine || Dead)
                return;
            TakeDamage(damage);
            Cache.PhotonView.RPC("NotifyDamagedRPC", PhotonTargets.All, new object[] { -1, name, damage });
            if (CurrentHealth <= 0f)
            {
                RPCManager.PhotonView.RPC("ShowKillFeedRPC", PhotonTargets.All, new object[] { name, Name, damage });
                Cache.PhotonView.RPC("NotifyDieRPC", PhotonTargets.All, new object[] { -1, name });
            }
        }

        [RPC]
        public virtual void GetKilledRPC(string name)
        {
            if (!Cache.PhotonView.isMine || Dead)
                return;
            SetCurrentHealth(0);
            RPCManager.PhotonView.RPC("ShowKillFeedRPC", PhotonTargets.All, new object[] { name, Name, 0 });
            Cache.PhotonView.RPC("NotifyDieRPC", PhotonTargets.All, new object[] { -1, name });
        }

        [RPC]
        public virtual void MarkDeadRPC(PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            Dead = true;
        }

        [RPC]
        public void NotifyDieRPC(int viewId, string name, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            var killer = Util.FindCharacterByViewId(viewId);
            if (killer != null)
                name = killer.Name;
            if (killer != null)
            {
                if (killer.IsMainCharacter())
                    _inGameManager.RegisterMainCharacterKill(this);
            }
            CustomLogicManager.Evaluator.OnCharacterDie(this, killer, name);
        }

        [RPC]
        public void NotifyDamagedRPC(int viewId, string name, int damage, PhotonMessageInfo info)
        {
            if (info.sender != Cache.PhotonView.owner)
                return;
            var killer = Util.FindCharacterByViewId(viewId);
            if (killer != null)
                name = killer.Name;
            if (killer != null)
            {
                if (killer.IsMainCharacter())
                    _inGameManager.RegisterMainCharacterDamage(this, damage);
            }
            if (damage > 0)
                CustomLogicManager.Evaluator.OnCharacterDamaged(this, killer, name, damage);
            if (SettingsManager.UISettings.GameFeed.Value)
            {
                string keyword = " killed ";
                if (CurrentHealth > 0)
                    keyword = " damaged ";
                string feed = ChatManager.GetColorString("(" + Util.FormatFloat(CustomLogicManager.Evaluator.CurrentTime, 2) + ") ", ChatTextColor.System) + name +
                    keyword + Name + " (" + damage.ToString() + ")";
                ChatManager.AddFeed(feed);
            }
        }

        public virtual void GetHit(BaseCharacter enemy, int damage, string type, string collider)
        {
            int viewId = -1;
            if (enemy != null)
                viewId = enemy.Cache.PhotonView.viewID;
            Cache.PhotonView.RPC("GetHitRPC", Cache.PhotonView.owner, new object[] { viewId, "", damage, type, collider });
        }

        public virtual void GetHit(string name, int damage, string type, string collider)
        {
            if (!Dead)
                Cache.PhotonView.RPC("GetHitRPC", Cache.PhotonView.owner, new object[] { -1, name, damage, type, collider });
        }

        public virtual void GetDamaged(string name, int damage)
        {
            if (!Dead)
                Cache.PhotonView.RPC("GetDamagedRPC", Cache.PhotonView.owner, new object[] { name, damage });
        }

        public virtual void GetKilled(string name)
        {
            if (!Dead)
                Cache.PhotonView.RPC("GetKilledRPC", Cache.PhotonView.owner, new object[] { name });
        }

        protected virtual void Awake()
        {
            if (SceneLoader.CurrentGameManager is InGameManager)
                _inGameManager = (InGameManager)SceneLoader.CurrentGameManager;
            CreateCache(null);
            SetColliders();
            CurrentHealth = MaxHealth = DefaultMaxHealth;
        }

        protected virtual void CreateCharacterIcon()
        {
        }

        protected virtual BaseMovementSync CreateMovementSync()
        {
            return gameObject.AddComponent<BaseMovementSync>();
        }

        protected virtual void SetColliders()
        {
        }

        protected virtual void Start()
        {
            MinimapHandler.CreateMinimapIcon(this);
        }

        public string GetCurrentAnimation()
        {
            foreach (AnimationState state in Cache.Animation)
            {
                if (Cache.Animation.IsPlaying(state.name))
                    return state.name;
            }
            return "";
        }

        public virtual Quaternion GetTargetRotation()
        {
            return Quaternion.Euler(0f, TargetAngle, 0f);
        }

        public virtual Vector3 GetTargetDirection()
        {
            float angleRadians = (90f - TargetAngle) * Mathf.Deg2Rad;
            var v = new Vector3(Mathf.Cos(angleRadians), 0f, Mathf.Sin(angleRadians));
            return v.normalized;
        }

        protected float GetAngleToTarget(Vector3 target)
        {
            Vector3 to = target - Cache.Transform.position;
            float angleX = -Mathf.Atan2(to.z, to.x) * Mathf.Rad2Deg;
            angleX = -Mathf.DeltaAngle(angleX, Cache.Transform.rotation.eulerAngles.y - 90f);
            return angleX;
        }

        protected virtual void CheckGround()
        {
            
            JustGrounded = false;
            if (CheckRaycastIgnoreTriggers(Cache.Transform.position + Vector3.up * 0.1f, -Vector3.up, GroundDistance, GroundMask.value))
            {
                if (!Grounded)
                    Grounded = JustGrounded = true;
            }
            else
                Grounded = false;
        }

        protected virtual bool CheckRaycastIgnoreTriggers(Vector3 origin, Vector3 direction, float distance, int layerMask)
        {
            return RaycastIgnoreTriggers(origin, direction, distance, layerMask).HasValue;
        }

        protected virtual RaycastHit? RaycastIgnoreTriggers(Vector3 origin, Vector3 direction, float distance, int layerMask)
        {
            var hits = Physics.RaycastAll(origin, direction, distance, GroundMask.value);
            foreach (var hit in hits)
            {
                if (!hit.collider.isTrigger)
                    return hit;
            }
            return null;
        }

        protected virtual void ToggleSound(string sound, bool toggle)
        {
            if (toggle && !Cache.AudioSources[sound].isPlaying)
                PlaySound(sound);
            else if (!toggle && Cache.AudioSources[sound].isPlaying)
                StopSound(sound);
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void LateUpdate()
        {
            LateUpdateFootstep();
        }

        protected virtual void LateUpdateFootstep()
        {
            int phase = GetFootstepPhase();
            string audio = GetFootstepAudio(_stepPhase);
            if (_stepPhase != phase && audio != "")
            {
                _stepPhase = phase;
                StopSoundRPC(audio, null);
                PlaySoundRPC(audio, null);
            }
        }

        protected virtual int GetFootstepPhase()
        {
            return 0;
        }

        protected virtual string GetFootstepAudio(int phase)
        {
            return "";
        }
    }
}
