using Content.Server.Atmos.Components;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Roles;
using Content.Server.Speech.Components;
using Content.Server.Temperature.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.RoundEnd;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.HeadSlime;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Content.Server.Actions;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Content.Shared.Actions.ActionTypes;
using System.Numerics;
using Robust.Shared.Map;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Server.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;

namespace Content.Server.HeadSlime
{
    /// <summary>
    ///     Handles HeadSlime propagation and inherent HeadSlime traits
    /// </summary>
    public sealed partial class HeadSlimeSystem
    {
        [Dependency] private readonly NpcFactionSystem _faction = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
        [Dependency] private readonly IChatManager _chatMan = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly SharedBodySystem _body = default!;

        /// <summary>
        ///     This is the general purpose function to call if you want to head slime an entity.
        ///     It handles only humanoids.
        /// </summary>
        /// <param name="target">the entity being Head Slimed</param>
        /// /// /// <param name="mobState"></param>
        
        public void HeadSlimeEntity(EntityUid target, MobStateComponent? mobState = null, bool HeadSlimeQueen = false, bool PutHSRHatOn = false)
        {
            //Can't be HeadSlimed again
            if (HasComp<HeadSlimeComponent>(target))
                return;

            if (!Resolve(target, ref mobState, logMissing: false))
                return;

            //HeadSlime's only attach to Humanoids
            if (!HasComp<HumanoidAppearanceComponent>(target))
                return;

            //Checking if target needs a free hat slot.
            if(!HeadSlimeQueen)
            {
                if(!PutHSRHatOn)
                {
                    if (_inventory.TryGetSlotEntity(target, "head", out var _))
                        return;
                }
            }

            //Welcome to the Head Slime Club
            var HeadSlimecomp = AddComp<HeadSlimeComponent>(target);

            //HeadSlimes dont need to eat or drink but TableSalt becomes lethal
            RemComp<HungerComponent>(target);
            RemComp<ThirstComponent>(target);

            //Adds an allergy to TableSalt
            if (TryComp<BodyComponent>(target, out var body))
            {
                foreach (var (component, _) in _body.GetBodyOrganComponents<StomachComponent>(target, body))
                {
                    if(_entityManager.TryGetComponent<MetabolizerComponent>(component.Owner, out var metabolizer))
                    {
                        if (metabolizer.MetabolizerTypes != null) 
                        {
                            metabolizer?.MetabolizerTypes.Add("HeadSlime");
                        }
                    }
                }
            }

            //HeadSlime Humanoid
            if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp)) //huapcomp
            {
                //Head Slime Queens have special traits and dont get the slow down or visual cue that others have.
                if(!HeadSlimeQueen)
                {
                    //Quick check to see if your being dumb and putting a real head slime on your head
                    if(!PutHSRHatOn)
                    {
                        EntityUid headSlime = default;
                        headSlime = _entityManager.SpawnEntity("HeadSlimeRealHat", new EntityCoordinates(target, Vector2.Zero));
        
                        _inventory.TryEquip(target, headSlime, "head" , true, true);
                    }

                    //popup
                    _popup.PopupEntity(Loc.GetString("Head-Slime-transform", ("target", target)), target, PopupType.LargeCaution);
                }
            }

            //Make it sentient if there's no plater. Disabled for now.
            //MakeSentientCommand.MakeSentient(target, EntityManager);

            _mobThreshold.SetAllowRevives(target, true);
            //Heals the HeadSlime from all the damage it took while human
            if (TryComp<DamageableComponent>(target, out var damageablecomp))
                _damageable.SetAllDamage(target, damageablecomp, 0);
            _mobState.ChangeMobState(target, MobState.Alive);
            _mobThreshold.SetAllowRevives(target, false);

            var factionComp = EnsureComp<NpcFactionMemberComponent>(target);
            foreach (var id in new List<string>(factionComp.Factions))
            {
                _faction.RemoveFaction(target, id);
            }
            _faction.AddFaction(target, "HeadSlime");

            //He's gotta have a mind
            var mindComp = EnsureComp<MindContainerComponent>(target);
            if (_mind.TryGetMind(target, out var mind, mindComp) && _mind.TryGetSession(mind, out var session))
            {
                //Head Slime role for player manifest
                _mind.AddRole(mind, new HeadSlimeRole(mind, _protoManager.Index<AntagPrototype>(HeadSlimecomp.HeadSlimeRoleId)));

                // Notificate player about new role assignment
                _audio.PlayGlobal(HeadSlimecomp.GreetSoundNotification, session);

                //Greeting message for new head slimes
                if(!HeadSlimeQueen)
                _chatMan.DispatchServerMessage(session, Loc.GetString("Head-Slime-infection-greeting"));
                else
                _chatMan.DispatchServerMessage(session, Loc.GetString("Head-Slime-queen-greeting"));
            }
            else
            {
                var htn = EnsureComp<HTNComponent>(target);
                htn.RootTask = new HTNCompoundTask() {Task = "SimpleHostileCompound"};
                htn.Blackboard.SetValue(NPCBlackboard.Owner, target);
                _npc.WakeNPC(target, htn);
            }

            if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !mindComp.HasMind)
            {
                //Ghost Role Details
                var ghostRole = EnsureComp<GhostRoleComponent>(target);
                EnsureComp<GhostTakeoverAvailableComponent>(target);
                ghostRole.RoleName = Loc.GetString("Head-Slime-generic");
                ghostRole.RoleDescription = Loc.GetString("Head-Slime-role-desc");
                ghostRole.RoleRules = Loc.GetString("Head-Slime-role-rules");
            }

            if(HeadSlimeQueen)
            {
                // Make them a HeadSlime Queen and grant them their special actions
                MakeHeadSlimeQueen(target, HeadSlimecomp);
            }

            //HeadSlimes get slowdown once they convert
            _movementSpeedModifier.RefreshMovementSpeedModifiers(target);

            //HeadSlime gamemode stuff
            var ev = new EntityHeadSlimeedEvent(target);
            RaiseLocalEvent(target, ref ev, true);
        }

        private void MakeHeadSlimeQueen(EntityUid uid, HeadSlimeComponent component)
        {
            component.HeadSlimeQueen = true;
            
            var bsinfectAction = new EntityTargetAction(_prototypeManager.Index<EntityTargetActionPrototype>(component.BSInfectActionName));
            _action.AddAction(uid, bsinfectAction, null);

            var bsinjectAction = new EntityTargetAction(_prototypeManager.Index<EntityTargetActionPrototype>(component.BSInjectActionName));
            _action.AddAction(uid, bsinjectAction, null);
        }

        /// <summary>
        ///     This is the function to call if you want to unhead slime an entity.
        /// </summary>
        /// <param name="source">the entity having the HeadSlimeComponent</param>
        /// <param name="target">the entity you want to unhead slime (different from source in case of cloning, for example)</param>
        /// <remarks>
        ///     this currently only removes the Head Slime hat
        /// </remarks>
        public bool UnHeadSlime(EntityUid source, EntityUid target, HeadSlimeComponent? HeadSlimecomp)
        {
            if (!Resolve(source, ref HeadSlimecomp))
                return false;

            //TODO: This needs to be cleaned up, so HeadSlimeComponent can be removed correctly

            //Remove any hats the target is wearing, if they still are
            if (_inventory.TryGetSlotEntity(source, "head", out var _))
            {
                _inventory.TryUnequip(source, "head", true, true);
            }

            if (TryComp<BodyComponent>(source, out var body))
            {
                foreach (var (component, _) in _body.GetBodyOrganComponents<StomachComponent>(source, body))
                {
                    if(_entityManager.TryGetComponent<MetabolizerComponent>(component.Owner, out var metabolizer))
                    {
                        if (metabolizer.MetabolizerTypes != null) 
                        {
                            metabolizer?.MetabolizerTypes.Remove("HeadSlime");
                        }
                    }
                }
            }

            //Hardcoded at the moment to make the target 'friendly'
            _faction.RemoveFaction(source, "HeadSlime");
            _faction.AddFaction(source, "NanoTrasen");

            //Restore their Movement Speed
            HeadSlimecomp.HeadSlimeMovementSpeedDebuff = 1;
            _movementSpeedModifier.RefreshMovementSpeedModifiers(source);

            return true;
        }
    }   
}
