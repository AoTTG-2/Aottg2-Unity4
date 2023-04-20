using Settings;
using Characters;
using UnityEngine;
using System.Collections.Generic;
using SimpleJSONFixed;
using Utility;

namespace Controllers
{
    class BaseTitanAIController : BaseAIController
    {
        protected BaseTitan _titan;
        public TitanAIState AIState = TitanAIState.Idle;
        public float DetectRange;
        public float LOSDelayMin;
        public float LOSDelayMax;
        public float AttackMinRange;
        public float AttackMaxRange;
        public float FocusRange;
        public float ReactionTime;
        public float FocusTime;
        public float AttackDelayMin;
        public float AttackDelayMax;
        public float AttackWaitMin;
        public float AttackWaitMax;
        public float AttackCooldownMin;
        public float AttackCooldownMax;
        public float ChaseStraightMin;
        public float ChaseStraightMax;
        public float ChaseDodgeMin;
        public float ChaseDodgeMax;
        public float ChaseDodgeMinRange;
        public bool IsRun;
        public bool IsTurn;
        public float LOSAngle;
        public float TurnAngle;
        protected Vector3 _moveToPosition;
        protected bool _moveToActive;
        protected float _moveToRange;
        protected bool _moveToIgnoreEnemies;
        public List<string> AttackNames = new List<string>();
        public List<bool> AttackHumanOnly = new List<bool>();
        public List<float> AttackChances = new List<float>();
        public Dictionary<string, List<Vector3>> AttackMinRanges = new Dictionary<string, List<Vector3>>();
        public Dictionary<string, List<Vector3>> AttackMaxRanges = new Dictionary<string, List<Vector3>>();
        protected float _stateTimeLeft;
        protected float _reactionTimeLeft;
        protected float _focusTimeLeft;
        protected float _attackRange;
        protected BaseCharacter _enemy;
        protected AICharacterDetection _detection;
        protected bool CanSeeEnemy;
        protected string _attack;
        protected float _attackCooldownLeft;

        protected override void Awake()
        {
            base.Awake();
            _titan = GetComponent<BaseTitan>();
        }

        protected override void Start()
        {
            Idle();
        }

        public void MoveTo(Vector3 position, float range, bool ignore)
        {
            _moveToPosition = position;
            _moveToActive = true;
            _moveToRange = range;
            _moveToIgnoreEnemies = ignore;
        }

        public void CancelOrder()
        {
            _moveToActive = false;
            if (AIState == TitanAIState.ForcedIdle)
                Idle();
            _enemy = null;
        }

        public void ForceIdle(float time)
        {
            Idle();
            AIState = TitanAIState.ForcedIdle;
            _stateTimeLeft = time;
        }

        public virtual void Init(JSONNode data)
        {
            DetectRange = data["DetectRange"].AsFloat;
            AttackMinRange = data["AttackMinRange"].AsFloat;
            AttackMaxRange = data["AttackMaxRange"].AsFloat;
            LOSDelayMin = data["LOSDelayMin"].AsFloat;
            LOSDelayMax = data["LOSDelayMax"].AsFloat;
            FocusRange = data["FocusRange"].AsFloat;
            ReactionTime = data["ReactionTime"].AsFloat;
            FocusTime = data["FocusTime"].AsFloat;
            AttackDelayMin = data["AttackDelayMin"].AsFloat;
            AttackDelayMax = data["AttackDelayMax"].AsFloat;
            AttackWaitMin = data["AttackWaitMin"].AsFloat;
            AttackWaitMax = data["AttackWaitMax"].AsFloat;
            AttackCooldownMin = data["AttackCooldownMin"].AsFloat;
            AttackCooldownMax = data["AttackCooldownMax"].AsFloat;
            ChaseStraightMin = data["ChaseStraightMin"].AsFloat;
            ChaseStraightMax = data["ChaseStraightMax"].AsFloat;
            ChaseDodgeMin = data["ChaseDodgeMin"].AsFloat;
            ChaseDodgeMax = data["ChaseDodgeMax"].AsFloat;
            ChaseDodgeMinRange = data["ChaseDodgeMinRange"].AsFloat;
            IsRun = data["IsRun"].AsBool;
            IsTurn = data["IsTurn"].AsBool;
            LOSAngle = data["LOSAngle"].AsFloat;
            TurnAngle = data["TurnAngle"].AsFloat;
            foreach (string attack in data["Attacks"].Keys)
            {
                float chance = data["Attacks"][attack];
                AttackNames.Add(attack);
                AttackChances.Add(chance);
                var node = data["AttackRanges"][attack];
                AttackMinRanges.Add(attack, new List<Vector3>());
                AttackMaxRanges.Add(attack, new List<Vector3>());
                for (int i = 0; i < node["Ranges"].Count; i++)
                {
                    var range = node["Ranges"][i];
                    AttackMinRanges[attack].Add(new Vector3(range["X"][0].AsFloat, range["Y"][0].AsFloat, range["Z"][0].AsFloat));
                    AttackMaxRanges[attack].Add(new Vector3(range["X"][1].AsFloat, range["Y"][1].AsFloat, range["Z"][1].AsFloat));
                }
               
                AttackHumanOnly.Add(node["HumanOnly"].AsBool);
            }
            _detection = AICharacterDetection.Create(_titan, DetectRange);
        }

        public void SetDetectRange(float range)
        {
            _detection.SetRange(range);
            DetectRange = range;
        }

        public void SetEnemy(BaseCharacter enemy, float focusTime = 0f)
        {
            _enemy = enemy;
            CanSeeEnemy = false;
            if (focusTime == 0f)
                focusTime = FocusTime;
            _focusTimeLeft = focusTime;
        }

        protected override void Update()
        {
            _reactionTimeLeft -= Time.deltaTime;
            _focusTimeLeft -= Time.deltaTime;
            _stateTimeLeft -= Time.deltaTime;
            if (_titan.Dead)
                return;
            if (_titan.State != TitanState.Attack && _titan.State != TitanState.Eat)
                _attackCooldownLeft -= Time.deltaTime;
            if (AIState == TitanAIState.ForcedIdle)
            {
                if (_stateTimeLeft <= 0f)
                    Idle();
                else
                    return;
            }
            if (_enemy != null)
            {
                if (_enemy.Dead)
                    _enemy = null;
                else
                {
                    Vector3 grounded = _enemy.Cache.Transform.position;
                    grounded.y = _titan.Cache.Transform.position.y;
                    bool canSeeEnemy = Mathf.Abs(Vector3.Angle((grounded - _titan.Cache.Transform.position).normalized, _titan.Cache.Transform.forward)) <= LOSAngle;
                    if (CanSeeEnemy)
                    {
                        if (!canSeeEnemy)
                        {
                            bool inRange = Util.DistanceIgnoreY(_character.Cache.Transform.position, _enemy.Cache.Transform.position) <= _attackRange;
                            if (!inRange || GetValidAttacks().Count == 0)
                            {
                                _enemy = null;
                                _reactionTimeLeft = Random.Range(LOSDelayMin, LOSDelayMax);
                            }
                        }
                    }
                    else
                    {
                        CanSeeEnemy = canSeeEnemy;
                    }
                }
            }
            if (_reactionTimeLeft <= 0f)
            {
                if (_enemy == null)
                {
                    _enemy = FindNearestEnemy();
                    CanSeeEnemy = false;
                    _focusTimeLeft = FocusTime;
                }
                _reactionTimeLeft = ReactionTime;
            }
            else if (_focusTimeLeft <= 0f && _enemy != null)
            {
                var newEnemy = FindNearestEnemy();
                if (newEnemy != null)
                {
                    _enemy = newEnemy;
                    CanSeeEnemy = false;
                }
                else
                {
                    if (Vector3.Distance(_titan.Cache.Transform.position, _enemy.Cache.Transform.position) > FocusRange)
                        _enemy = null;
                }
                _focusTimeLeft = FocusTime;
            }
            _titan.TargetEnemy = _enemy;
            if (_moveToActive && _moveToIgnoreEnemies)
                _enemy = null;
            if (AIState == TitanAIState.Idle || AIState == TitanAIState.Wander || AIState == TitanAIState.SitIdle)
            {
                if (_enemy == null)
                {
                    if (_moveToActive)
                        MoveToPosition();
                    else if (_stateTimeLeft <= 0f)
                    {
                        if (AIState == TitanAIState.Idle)
                        {
                            if (!IsCrawler() && RandomGen.Roll(0.3f))
                                Sit();
                            else
                                Wander();
                        }
                        else
                            Idle();
                    }
                }
                else
                {
                    _attackRange = Random.Range(AttackMinRange * _titan.Size, AttackMaxRange * _titan.Size);
                    MoveToEnemy(true);
                }
            }
            else if (AIState == TitanAIState.MoveToPosition)
            {
                float distance = Vector3.Distance(_character.Cache.Transform.position, _moveToPosition);
                if (distance < _moveToRange || !_moveToActive)
                {
                    _moveToActive = false;
                    Idle();
                }
                else if (_stateTimeLeft <= 0)
                    MoveToPositionDodge();
                else
                    _titan.TargetAngle = GetTargetAngle((_moveToPosition - _character.Cache.Transform.position).normalized);
            }
            else if (AIState == TitanAIState.MoveToPositionDodge)
            {
                float distance = Vector3.Distance(_character.Cache.Transform.position, _moveToPosition);
                if (distance < _moveToRange || !_moveToActive)
                {
                    _moveToActive = false;
                    Idle();
                }
                else if (_stateTimeLeft <= 0f)
                {
                    MoveToPosition();
                }
            }
            else if (AIState == TitanAIState.MoveToEnemy)
            {
                if (_enemy == null)
                    Idle();
                else if (_stateTimeLeft <= 0f && Util.DistanceIgnoreY(_character.Cache.Transform.position, _enemy.Cache.Transform.position) > ChaseDodgeMinRange)
                    MoveToEnemyDodge();
                else
                {
                    bool inRange = Util.DistanceIgnoreY(_character.Cache.Transform.position, _enemy.Cache.Transform.position) <= _attackRange;
                    if (_attackCooldownLeft <= 0f && inRange && GetValidAttacks().Count > 0)
                        WaitAttack();
                    else
                        _titan.TargetAngle = GetTargetAngle((_enemy.Cache.Transform.position - _character.Cache.Transform.position).normalized);
                }
            }
            else if (AIState == TitanAIState.MoveToEnemyDodge)
            {
                if (_enemy == null && _stateTimeLeft <= 0f)
                    Idle();
                else if (_enemy != null)
                {
                    bool inRange = Util.DistanceIgnoreY(_character.Cache.Transform.position, _enemy.Cache.Transform.position) <= _attackRange;
                    if (_attackCooldownLeft <= 0f && inRange && GetValidAttacks().Count > 0)
                        WaitAttack();
                    else
                    {
                        inRange = Util.DistanceIgnoreY(_character.Cache.Transform.position, _enemy.Cache.Transform.position) <= ChaseDodgeMinRange;
                        if (_stateTimeLeft <= 0f || inRange)
                            MoveToEnemy(false);
                    }
                }
            }
            else if (AIState == TitanAIState.WaitAttack)
            {
                if (_enemy == null)
                {
                    Idle();
                    return;
                }
                if (_stateTimeLeft <= 0f)
                {
                    bool inRange = Util.DistanceIgnoreY(_character.Cache.Transform.position, _enemy.Cache.Transform.position) <= _attackRange;
                    if (!inRange || GetValidAttacks().Count == 0)
                        Idle();
                    else
                        PrepareAttack();
                }
            }
            else if (AIState == TitanAIState.DelayAttack)
            {
                if (_enemy == null)
                {
                    Idle();
                    return;
                }
                if (_stateTimeLeft <= 0f)
                    Attack();
            }
            else if (AIState == TitanAIState.Action)
            {
                if (_titan.State == TitanState.Idle)
                    Idle();
            }
        }

        protected bool IsCrawler()
        {
            return _titan is BasicTitan && ((BasicTitan)_titan).IsCrawler;
        }

        protected void Idle()
        {
            AIState = TitanAIState.Idle;
            _titan.HasDirection = false;
            _titan.IsSit = false;
            _stateTimeLeft = Random.Range(2f, 6f);
        }

        protected void Wander()
        {
            AIState = TitanAIState.Wander;
            _titan.HasDirection = true;
            _titan.TargetAngle = Random.Range(0f, 360f);
            _titan.IsWalk = true;
            _titan.IsSit = false;
            if (IsCrawler())
                _titan.IsWalk = false;
            float angle = Vector3.Angle(_titan.Cache.Transform.forward, _titan.GetTargetDirection());
            if (Mathf.Abs(angle) > 60f)
                _titan.Turn(_titan.GetTargetDirection());
            _stateTimeLeft = Random.Range(2f, 8f);
        }

        protected void Sit()
        {
            AIState = TitanAIState.SitIdle;
            _titan.IsSit = true;
            _stateTimeLeft = Random.Range(6f, 12f);
        }

        protected void MoveToEnemy(bool turn)
        {
            AIState = TitanAIState.MoveToEnemy;
            bool inRange = Util.DistanceIgnoreY(_character.Cache.Transform.position, _enemy.Cache.Transform.position) <= _attackRange;
            _titan.HasDirection = true;
            _titan.IsSit = false;
            _titan.IsWalk = !IsRun;
            _titan.TargetAngle = GetTargetAngle((_enemy.Cache.Transform.position - _character.Cache.Transform.position).normalized);
            float angle = Vector3.Angle(_titan.Cache.Transform.forward, _titan.GetTargetDirection());
            _stateTimeLeft = Random.Range(ChaseStraightMin, ChaseStraightMax);
            if (Mathf.Abs(angle) > TurnAngle && IsTurn && turn)
                _titan.Turn(_titan.GetTargetDirection());
            else if (inRange && GetValidAttacks().Count > 0)
                WaitAttack();
        }

        protected void MoveToPosition()
        {
            AIState = TitanAIState.MoveToPosition;
            _titan.HasDirection = true;
            _titan.IsSit = false;
            _titan.IsWalk = !IsRun;
            _titan.TargetAngle = GetTargetAngle((_moveToPosition - _character.Cache.Transform.position).normalized);
            float angle = Vector3.Angle(_titan.Cache.Transform.forward, _titan.GetTargetDirection());
            if (Mathf.Abs(angle) > 60f && IsTurn)
                _titan.Turn(_titan.GetTargetDirection());
            _stateTimeLeft = Random.Range(ChaseStraightMin, ChaseStraightMax);
        }

        protected void MoveToEnemyDodge()
        {
            AIState = TitanAIState.MoveToEnemyDodge;
            _titan.HasDirection = true;
            _titan.IsSit = false;
            _titan.IsWalk = !IsRun;
            _titan.TargetAngle = GetTargetAngle((_enemy.Cache.Transform.position - _character.Cache.Transform.position).normalized);
            _titan.TargetAngle += Random.Range(-45f, 45f);
            if (_titan.TargetAngle > 360f)
                _titan.TargetAngle -= 360f;
            if (_titan.TargetAngle < 0f)
                _titan.TargetAngle += 360f;
            _stateTimeLeft = Random.Range(ChaseDodgeMin, ChaseDodgeMax);
        }

        protected void MoveToPositionDodge()
        {
            AIState = TitanAIState.MoveToPositionDodge;
            _titan.HasDirection = true;
            _titan.IsSit = false;
            _titan.IsWalk = !IsRun;
            _titan.TargetAngle = GetTargetAngle((_moveToPosition - _character.Cache.Transform.position).normalized);
            _titan.TargetAngle += Random.Range(-45f, 45f);
            if (_titan.TargetAngle > 360f)
                _titan.TargetAngle -= 360f;
            if (_titan.TargetAngle < 0f)
                _titan.TargetAngle += 360f;
            _stateTimeLeft = Random.Range(ChaseDodgeMin, ChaseDodgeMax);
        }

        protected void PrepareAttack()
        {
            AIState = TitanAIState.DelayAttack;
            _titan.HasDirection = false;
            string attack = GetRandomAttack();
            if (attack == "")
                Idle();
            else
            {
                _attack = attack;
                _stateTimeLeft = Random.Range(AttackDelayMin, AttackDelayMax);
            }
        }

        protected void Attack()
        {
            AIState = TitanAIState.Action;
            _titan.HasDirection = false;
            if (_titan.CanAttack())
            {
                _titan.Attack(_attack);
                _attackCooldownLeft = Random.Range(AttackCooldownMin, AttackCooldownMax);
            }
            else
                Idle();
        }

        protected void WaitAttack()
        {
            AIState = TitanAIState.WaitAttack;
            _titan.HasDirection = false;
            _stateTimeLeft = Random.Range(AttackWaitMin, AttackWaitMax);
        }

        protected BaseCharacter FindNearestEnemy()
        {
            if (_detection.Enemies.Count == 0)
                return null;
            Vector3 position = _titan.Cache.Transform.position;
            float nearestDistance = float.PositiveInfinity;
            BaseCharacter nearestCharacter = null;
            foreach (BaseCharacter character in _detection.Enemies)
            {
                if (character == null || character.Dead)
                    continue;
                float distance = Vector3.Distance(character.Cache.Transform.position, position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestCharacter = character;
                }
            }
            return nearestCharacter;
        }

        private string GetRandomAttack()
        {
            float total = 0f;
            var validAttacks = GetValidAttacks();
            foreach (int attack in validAttacks)
                total += AttackChances[attack];
            if (total == 0f)
                return string.Empty;
            float r = Random.Range(0f, total);
            float start = 0f;
            foreach (int attack in validAttacks)
            {
                if (r >= start && r < start + AttackChances[attack])
                    return AttackNames[attack];
                start += AttackChances[attack];
            }
            return AttackNames[validAttacks[0]];
        }

        protected virtual List<int> GetValidAttacks()
        {
            var attacks = new List<int>();
            if (_enemy == null)
                return attacks;
            Vector3 diff = _character.Cache.Transform.InverseTransformPoint(_enemy.Cache.Transform.position);
            bool isHuman = _enemy is Human;
            for (int i = 0; i < AttackNames.Count; i++)
            {
                string attack = AttackNames[i];
                if (AttackHumanOnly[i] && !isHuman)
                    continue;
                for (int j = 0; j < AttackMinRanges[attack].Count; j++)
                {
                    var min = AttackMinRanges[attack][j];
                    var max = AttackMaxRanges[attack][j];
                    if (diff.x >= min.x && diff.y >= min.y && diff.z >= min.z && diff.x <= max.x && diff.y <= max.y && diff.z <= max.z)
                    {
                        attacks.Add(i);
                        break;
                    }
                }
            }
            return attacks;
        }
    }

    public enum TitanAIState
    {
        Idle,
        Wander,
        SitIdle,
        MoveToEnemy,
        MoveToEnemyDodge,
        MoveToPosition,
        MoveToPositionDodge,
        Action,
        WaitAttack,
        DelayAttack,
        ForcedIdle
    }
}
