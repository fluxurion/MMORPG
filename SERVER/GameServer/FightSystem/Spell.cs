using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MMORPG.Common.Proto.Entity;
using MMORPG.Common.Proto.Fight;
using GameServer.Tool;
using Org.BouncyCastle.Asn1.X509;
using Serilog;
using GameServer.EntitySystem;
using GameServer.PlayerSystem;
using GameServer.MonsterSystem;

namespace GameServer.FightSystem
{
    /// <summary>
    /// Skill Releaser
    /// </summary>
    public class Spell
    {
        public Actor OwnerActor;
        public Skill? CurrentRunSkill;

        public Spell(Actor ownerActor)
        {
            OwnerActor = ownerActor;
        }

        public CastResult Cast(CastInfo info)
        {
            if ((OwnerActor.FlagState & FlagStates.Silence) == FlagStates.Silence || (OwnerActor.FlagState & FlagStates.Stun) == FlagStates.Stun)
            {
                return CastResult.NotAllowed;
            }

            if (CurrentRunSkill != null)
            {
                ResponseSpellFail(info, CastResult.Running);
                return CastResult.Running;
            }
            var skill = OwnerActor.SkillManager.GetSkill(info.SkillId);
            if (skill == null)
            {
                ResponseSpellFail(info, CastResult.InvalidSkillId);
                return CastResult.InvalidSkillId;
            }

            if (OwnerActor is Player player)
            {
                Log.Information($"{player}Request to release skills: TargetType:{skill.Define.TargetType}, {info}");
            }
            else if (OwnerActor is Monster monster)
            {
                Log.Information($"{monster}Request to release skills: TargetType:{skill.Define.TargetType}, {info}");
            }
            switch (skill.Define.TargetType)
            {
                case "None":
                    return CastNone(skill, info);
                case "Unit":
                    return CastUnit(skill, info);
                case "Position":
                    return CastPosition(skill, info);
                default:
                    Log.Error("[Spell.Cast]Invalid target type.");
                    return CastResult.TargetInvaild;
            }
        }

        // Release non-targeted skills
        private CastResult CastNone(Skill skill, CastInfo info)
        {
            var target = new CastTargetEntity(OwnerActor);
            return CastTarget(skill, info, target);
        }

        // Release unit-targeting skills
        private CastResult CastUnit(Skill skill, CastInfo info)
        {
            var targetActor = EntityManager.Instance.GetEntity(info.CastTarget.TargetId) as Actor;
            if (targetActor == null)
            {
                ResponseSpellFail(info, CastResult.InvalidCastTarget);
                return CastResult.InvalidCastTarget;
            }
            var target = new CastTargetEntity(targetActor);
            return CastTarget(skill, info, target);
        }

        // Release location-targeted skills
        private CastResult CastPosition(Skill skill, CastInfo info)
        {
            var target = new CastTargetPosition(info.CastTarget.TargetPos.ToVector3().ToVector2());
            return CastTarget(skill, info, target);
        }


        private CastResult CastTarget(Skill skill, CastInfo info, CastTarget target)
        {
            var res = skill.CanCast(target);
            ResponseSpellFail(info, res);
            if (res != CastResult.Success)
            {
                return res;
            }
            skill.Cast(target);
            skill.OwnerActor.Map.PlayerManager.Broadcast(new SpellResponse() { Info = info }, skill.OwnerActor);
            return res;
        }


        private void ResponseSpellFail(CastInfo info, CastResult result)
        {
            if (OwnerActor is Player player)
            {
                if (result != CastResult.Success)
                {
                    Log.Debug($"{player.User.Channel}An error occurred while requesting the attack: {result}");
                }
                player.User.Channel.Send(new SpellFailResponse()
                {
                    CasterId = info.CasterId,
                    SkillId = info.SkillId,
                    Reason = result
                });
            }
        }
    }
}
