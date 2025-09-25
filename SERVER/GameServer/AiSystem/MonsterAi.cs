using System.Diagnostics;
using MMORPG.Common.Proto.Entity;
using GameServer.Tool;
using System.Numerics;
using GameServer.PlayerSystem;
using GameServer.MonsterSystem;
using GameServer.EntitySystem;
using GameServer.AiSystem.Ability;
using GameServer.Manager;
using GameServer.RewardSystem;
using MMORPG.Common.Proto.Fight;
using MMORPG.Common.Tool;
using System;

namespace GameServer.AiSystem
{
    public enum MonsterAiState
    {
        None = 0,
        Walk,
        Cast,
        Hurt,
        Chase,
        Goback,
        Death,
    }

    public class MonsterAbilityManager
    {
        public Monster OwnerMonster { get; }
        public AnimationState AnimationState;
        public IdleAbility IdleAbility { get; }
        public MoveAbility MoveAbility { get; }
        public CastSkillAbility CastSkillAbility { get; }

        public Actor? ChasingTarget;
        public Random Random = new();

        // Range of activity relative to the spawn point
        public float WalkRange;
        // Pursuit range relative to spawn point
        public float ChaseRange;
        // Attack range
        public float AttackRange;

        public MonsterAbilityManager(Monster ownerMonster)
        {
            OwnerMonster = ownerMonster;
            MoveAbility = new(OwnerMonster, OwnerMonster.InitPos.Y, OwnerMonster.Speed);
            IdleAbility = new();
            CastSkillAbility = new(OwnerMonster);

            WalkRange = OwnerMonster.SpawnDefine.WalkRange;
            ChaseRange = OwnerMonster.SpawnDefine.ChaseRange;
            AttackRange = OwnerMonster.SpawnDefine.AttackRange;
        }

        public void Start()
        {
            IdleAbility.Start();
            MoveAbility.Start();
        }

        public void Update()
        {
            if (AnimationState == AnimationState.Move)
            {
                MoveAbility.Update();
                if (MoveAbility.Moving)
                {
                    UpdateAnimationState();
                }
                else
                {
                    Idle();
                }
            }
            else if (AnimationState == AnimationState.Idle)
            {
                IdleAbility.Update();
            }
        }

        public void Move(Vector2 destination)
        {
            if (AnimationState == AnimationState.Idle)
            {
                AnimationState = AnimationState.Move;
            }
            MoveAbility.Move(destination);
        }

        public void AddForce(Vector2 force)
        {
            if (AnimationState == AnimationState.Idle)
            {
                AnimationState = AnimationState.Move;
            }
            MoveAbility.AddForce(force);
        }

        public void Idle()
        {
            ChangeAnimationState(AnimationState.Idle);
        }

        public void CastSkill()
        {
            ChangeAnimationState(AnimationState.Skill);
            AnimationState = AnimationState.Idle;

            if (!OwnerMonster.SkillManager.SkillDict.Any() || ChasingTarget == null) return;
            var values = OwnerMonster.SkillManager.SkillDict.Values.ToList();
            var skill  = values[Random.Next(values.Count)];
            if (CastSkillAbility.CastSkill(skill.Define.ID, ChasingTarget) == CastResult.Success) ;
        }

        public void Revive()
        {
            OwnerMonster.ChangeHp(OwnerMonster.AttributeManager.Final.MaxHp);
        }

        public void OnHurt()
        {
            ChangeAnimationState(AnimationState.Hurt);
            AnimationState = AnimationState.Idle;

            Debug.Assert(OwnerMonster.DamageSourceInfo != null);
            var attackInfo = OwnerMonster.DamageSourceInfo.AttackerInfo;

            switch (attackInfo.AttackerType)
            {
                case AttackerType.Skill:
                    // Can get the attacker, apply a force
                    var skillDefine = DataManager.Instance.SkillDict[attackInfo.SkillId];
                    var target = EntityManager.Instance.GetEntity(attackInfo.AttackerId);
                    if (target == null) return;

                    var direction = OwnerMonster.Position - target.Position;
                    AddForce(direction.Normalized() * skillDefine.Force);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnDeath()
        {
            MoveAbility.ClearForce();
            OwnerMonster.ZeroFlagState();
            ChangeAnimationState(AnimationState.Death);

            // dropped items
            var killRewardList = DataHelper.ParseIntegers(OwnerMonster.SpawnDefine.killRewardList);
            foreach (var rewardId in killRewardList)
            {
                var reward = DataManager.Instance.RewardDict[rewardId];
                if (reward.Type == "DropItem")
                {
                    RewardManager.Instance.Distribute(rewardId, OwnerMonster);
                }
                else if (reward.Type == "InventoryItem" || reward.Type == "Buff")
                {
                    if (OwnerMonster.DamageSourceInfo == null) continue;
                    var entity =
                        EntityManager.Instance.GetEntity(OwnerMonster.DamageSourceInfo.AttackerInfo.AttackerId);
                    if (entity == null || !(entity is Player)) continue;
                    RewardManager.Instance.Distribute(rewardId, entity);
                }
            }

            // Drop experience
            {
                if (OwnerMonster.DamageSourceInfo == null) return;
                var entity =
                    EntityManager.Instance.GetEntity(OwnerMonster.DamageSourceInfo.AttackerInfo.AttackerId);
                var exp = (int)(OwnerMonster.UnitDefine.DropExpBase *
                          Math.Pow(OwnerMonster.Level, OwnerMonster.UnitDefine.DropExpLevelFactor));
                if (entity == null || !(entity is Player player)) return;
                player.ChangeExp(exp);
            }
        }

        private void ChangeAnimationState(AnimationState state)
        {
            if (AnimationState == state) return;
            AnimationState = state;
            UpdateAnimationState();
        }

        private void UpdateAnimationState()
        {
            var res = new EntityTransformSyncResponse()
            {
                EntityId = OwnerMonster.EntityId,
                StateId = (int)AnimationState,
                Transform = ProtoHelper.ToNetTransform(OwnerMonster.Position, OwnerMonster.Direction)
            };
            OwnerMonster.Map.PlayerManager.Broadcast(res, OwnerMonster);
        }
    }

    public class MonsterAi : AiBase
    {
        public FSM<MonsterAiState> Fsm = new();
        public MonsterAbilityManager AbilityManager;

        public MonsterAi(Monster monster)
        {
            AbilityManager = new MonsterAbilityManager(monster);
            Fsm.AddState(MonsterAiState.Walk, new WalkState(Fsm, AbilityManager));
            Fsm.AddState(MonsterAiState.Chase, new ChaseState(Fsm, AbilityManager));
            Fsm.AddState(MonsterAiState.Cast, new CastState(Fsm, AbilityManager));
            Fsm.AddState(MonsterAiState.Goback, new GobackState(Fsm, AbilityManager));
            Fsm.AddState(MonsterAiState.Hurt, new HurtState(Fsm, AbilityManager));
            Fsm.AddState(MonsterAiState.Death, new DeathState(Fsm, AbilityManager));
        }

        public override void Start()
        {
            AbilityManager.Start();
            Fsm.ChangeState(MonsterAiState.Walk);
        }

        public override void Update()
        {
            AbilityManager.Update();
            Fsm.Update();
        }

        /// <summary>
        /// patrol status
        /// </summary>
        public class WalkState : FSMAbstractState<MonsterAiState, MonsterAbilityManager>
        {
            private float _lastTime;
            private float _waitTime;
            private Vector2 _lastRandomPointWithBirth;

            public WalkState(FSM<MonsterAiState> fsm, MonsterAbilityManager parameter) :
                base(fsm, parameter)
            {
                _waitTime = _target.Random.NextSingle() * 10;
            }

            public override void OnEnter()
            {
                _lastTime = Time.time;
                if (_target.AnimationState != AnimationState.Move)
                {
                    _target.Idle();
                }
            }

            public override void OnUpdate()
            {
                var monster = _target.OwnerMonster;

                if (monster.DamageSourceInfo != null && !monster.DamageSourceInfo.IsMiss)
                {
                    _fsm.ChangeState(MonsterAiState.Hurt);
                    return;
                }
                if (monster.IsDeath())
                {
                    _fsm.ChangeState(MonsterAiState.Death);
                    return;
                }

                // Find the player closest to the monster within the monster's field of view
                var nearestPlayer = monster.Map.GetEntityFollowingNearest(monster, 
                    e =>
                    {
                        if (e.EntityType != EntityType.Player) return false;
                        var player = (Player)e;
                        return player.IsValid() && !player.IsDeath();
                    });

                if (nearestPlayer != null)
                {
                    // If the player is within the monster's pursuit range
                    float d1 = Vector2.Distance(monster.InitPos, nearestPlayer.Position); // Distance from target to spawn point
                    float d2 = Vector2.Distance(monster.Position, nearestPlayer.Position); // The distance between yourself and the target
                    if (d1 <= _target.ChaseRange && d2 <= _target.ChaseRange)
                    {
                        // Switch to pursuit mode
                        _target.ChasingTarget = nearestPlayer as Actor;
                        _fsm.ChangeState(MonsterAiState.Chase);
                        return;
                    }
                }

                if (_target.AnimationState != AnimationState.Idle) return;
                if (!(_lastTime + _waitTime < Time.time)) return;

                _lastRandomPointWithBirth = RandomPointWithBirth(_target.WalkRange);

                // If the state is idle or the waiting time has expired, try to move randomly
                _waitTime = _target.Random.NextSingle() * 10;
                _lastTime = Time.time;
                _target.Move(_lastRandomPointWithBirth);
            }


            public Vector2 RandomPointWithBirth(float range)
            {
                var monster = _target.OwnerMonster;
                float x = _target.Random.NextSingle() * 2f - 1f;
                float y = _target.Random.NextSingle() * 2f - 1f;
                var direction = new Vector2(x, y).Normalized();
                return monster.InitPos + direction * range * _target.Random.NextSingle();
            }
        }

        /// <summary>
        /// Skill release status
        /// </summary>
        public class CastState : FSMAbstractState<MonsterAiState, MonsterAbilityManager>
        {
            private float _endTime;
            private DamageInfo _currentDamageInfo;

            public CastState(FSM<MonsterAiState> fsm, MonsterAbilityManager parameter) :
                base(fsm, parameter)
            {
            }

            public override void OnEnter()
            {
                _target.CastSkill();
            }

            public override void OnUpdate()
            {
                if (_target.OwnerMonster.Spell.CurrentRunSkill == null)
                {
                    _fsm.ChangeState(MonsterAiState.Walk);
                }
            }
        }

        /// <summary>
        /// Hit status
        /// </summary>
        public class HurtState : FSMAbstractState<MonsterAiState, MonsterAbilityManager>
        {
            private float _endTime;
            private DamageInfo _currentDamageInfo;

            public HurtState(FSM<MonsterAiState> fsm, MonsterAbilityManager parameter) :
                base(fsm, parameter)
            {
            }

            public override void OnEnter()
            {
                _target.MoveAbility.LockDirection = true;

                _endTime = Time.time + _target.OwnerMonster.UnitDefine.HurtTime;
                Debug.Assert(_target.OwnerMonster.DamageSourceInfo != null);
                _currentDamageInfo = _target.OwnerMonster.DamageSourceInfo;
                _target.OnHurt();
            }

            public override void OnExit()
            {
                _target.MoveAbility.LockDirection = false;
                if (!_target.OwnerMonster.IsDeath())
                {
                    _target.OwnerMonster.DamageSourceInfo = null;
                }
            }

            public override void OnUpdate()
            {
                if (_target.OwnerMonster.IsDeath())
                {
                    _fsm.ChangeState(MonsterAiState.Death);
                    return;
                }
                if (!(_endTime < Time.time)) return;
                if (_currentDamageInfo != _target.OwnerMonster.DamageSourceInfo)
                {
                    OnEnter();
                    return;
                }
                _fsm.ChangeState(MonsterAiState.Walk);
            }
        }

        /// <summary>
        /// pursuit status
        /// </summary>
        public class ChaseState : FSMAbstractState<MonsterAiState, MonsterAbilityManager>
        {
            public ChaseState(FSM<MonsterAiState> fsm, MonsterAbilityManager parameter) :
                base(fsm, parameter)
            { }

            public override void OnUpdate()
            {
                var monster = _target.OwnerMonster;
                if (monster.DamageSourceInfo != null && !monster.DamageSourceInfo.IsMiss)
                {
                    _fsm.ChangeState(MonsterAiState.Hurt);
                    return;
                }
                if (monster.IsDeath())
                {
                    _fsm.ChangeState(MonsterAiState.Death);
                    return;
                }
                if (_target.ChasingTarget == null)// || monster.ChasingTarget.IsDeath())
                {
                    _fsm.ChangeState(MonsterAiState.Goback);
                    return;
                }

                var player = _target.ChasingTarget as Player;
                if (player == null || !player.IsValid() || player.IsDeath())
                {
                    _fsm.ChangeState(MonsterAiState.Goback);
                    return;
                }

                float d1 = Vector2.Distance(monster.Position, player.Position);  // The distance between yourself and the target
                float d2 = Vector2.Distance(monster.Position, monster.InitPos); // The distance between the user and the birth point
                if (d1 > _target.ChaseRange || d2 > _target.ChaseRange)
                {
                    _fsm.ChangeState(MonsterAiState.Goback);
                    return;
                }

                if (d1 <= _target.AttackRange)
                {
                    // The distance is enough, you can try to release the skill
                    _fsm.ChangeState(MonsterAiState.Cast);
                    return;
                }
                else
                {
                    _target.Move(player.Position);
                }
            }
        }

        /// <summary>
        /// return status
        /// </summary>
        public class GobackState : FSMAbstractState<MonsterAiState, MonsterAbilityManager>
        {
            public GobackState(FSM<MonsterAiState> fsm, MonsterAbilityManager parameter) :
                base(fsm, parameter)
            { }

            public override void OnEnter()
            {
                _target.Move(_target.OwnerMonster.InitPos);

                // Switch back to patrol mode, so that you can continue to search for enemies while returning to the birth point
                _fsm.ChangeState(MonsterAiState.Walk);
            }
        }

        /// <summary>
        /// state of death
        /// </summary>
        public class DeathState : FSMAbstractState<MonsterAiState, MonsterAbilityManager>
        {
            public DeathState(FSM<MonsterAiState> fsm, MonsterAbilityManager parameter) :
                base(fsm, parameter)
            { }

            public override void OnEnter()
            {
                _target.OnDeath();
            }

            public override void OnExit()
            {
                _target.OwnerMonster.DamageSourceInfo = null;
            }

            public override void OnUpdate()
            {
                if (_target.OwnerMonster.IsDeath()) return;
                _fsm.ChangeState(MonsterAiState.Walk);
            }
        }

    }
}
