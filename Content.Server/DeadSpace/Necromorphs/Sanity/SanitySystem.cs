// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Content.Shared.Popups;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.NPC;
using Content.Server.Chat.Managers;
using Content.Shared.Ghost;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.Jittering;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Stunnable;

namespace Content.Server.DeadSpace.Necromorphs.Sanity
{
    public sealed class SanitySystem : SharedSanitySystem
    {
        [Dependency] private readonly IChatManager _chatMan = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly GhostSystem _ghosts = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly NpcFactionSystem _faction = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
        [Dependency] private readonly SharedJitteringSystem _sharedJittering = default!;
        [Dependency] private readonly SlurredSystem _slurred = default!;
        private const string HighSanityMessage = "Вы чувствуете головную боль";
        private const string MediumSanityMessage = "У вас болит голова, кости будто ломаются на части";
        private const string LowSanityMessage = "Вы теряете рассудок, вам совсем плохо!";
        private const string LostSanityMessage = "Вы теряете сознание.";

        [ValidatePrototypeId<StatusEffectPrototype>]
        public const string SlowedDownKey = "SlowedDown";
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SanityComponent, SanityEvent>(OnSanity);
            SubscribeLocalEvent<SanityComponent, CheckCrazyMobEvent>(CheckCrazyMob);
            SubscribeLocalEvent<SanityComponent, ComponentShutdown>(OnSanityShutdown);
        }
        private void OnSanityShutdown(EntityUid uid, SanityComponent comp, ComponentShutdown args)
        {
            if (!TryComp<GhostComponent>(comp.Ghost, out _))
                return;

            if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                return;

            _mindSystem.UnVisit(mindId, mind);

            UnCrazy(uid, comp);
        }
        private void OnSanity(EntityUid uid, SanityComponent comp, ref SanityEvent args)
        {
            if (comp.SanityLevel >= comp.MaxSanityLevel)
                return;

            float highSanityThreshold = comp.MaxSanityLevel * 0.66f;   // Верхние 33%
            float mediumSanityThreshold = comp.MaxSanityLevel * 0.33f; // Средние 33%
            float lowSanityThreshold = 0;                              // Нижние 33%

            switch (comp.SanityLevel)
            {
                case float sanityLevel when sanityLevel > highSanityThreshold:
                    _popup.PopupEntity(HighSanityMessage, uid, uid);
                    HighSanity(uid, comp);
                    break;
                case float sanityLevel when sanityLevel <= highSanityThreshold && sanityLevel > mediumSanityThreshold:
                    _popup.PopupEntity(MediumSanityMessage, uid, uid);
                    MediumSanity(uid, comp);
                    break;
                case float sanityLevel when sanityLevel <= mediumSanityThreshold && sanityLevel > lowSanityThreshold:
                    _popup.PopupEntity(LowSanityMessage, uid, uid);
                    LowSanity(uid, comp);
                    break;
                case float sanityLevel when sanityLevel <= 0:
                    _popup.PopupEntity(LostSanityMessage, uid, uid);
                    break;
                default:
                    break;
            }
        }
        private void LowSanity(EntityUid uid, SanityComponent comp)
        {
            MediumSanity(uid, comp);
            HighSanity(uid, comp);
            _slurred.DoSlur(uid, TimeSpan.FromSeconds(comp.UpdateDuration + 1));
            _statusEffect.TryAddStatusEffect<SlowedDownComponent>(uid, SlowedDownKey, TimeSpan.FromSeconds(comp.UpdateDuration + 1), true);
        }
        private void MediumSanity(EntityUid uid, SanityComponent comp)
        {
            _sharedJittering.DoJitter(uid, TimeSpan.FromSeconds(comp.UpdateDuration + 1), true);
            HighSanity(uid, comp);
        }
        private void HighSanity(EntityUid uid, SanityComponent comp)
        {
            return;
        }
        private void CheckCrazyMob(EntityUid uid, SanityComponent comp, ref CheckCrazyMobEvent args)
        {
            if (comp.SanityLevel <= 0)
            {
                Crazy(uid, comp);
            }
            else
            {
                UnCrazy(uid, comp);
            }
        }
        private void Crazy(EntityUid uid, SanityComponent comp)
        {
            if (comp.IsCrazy)
                return;

            if (TryComp<NpcFactionMemberComponent>(uid, out var factionComp))
                comp.OldFaction = GetFirstElement(factionComp.Factions);

            if (TryComp<HTNComponent>(uid, out var hTNComponent))
                comp.OldTask = hTNComponent.RootTask.Task;

            var hasMind = _mindSystem.TryGetMind(uid, out var mindId, out var mind);

            RemComp<HTNComponent>(uid);
            var htn = EnsureComp<HTNComponent>(uid);
            htn.RootTask = new HTNCompoundTask() { Task = "SimpleHostileCompound" };
            htn.Blackboard.SetValue(NPCBlackboard.Owner, uid);
            _npc.WakeNPC(uid, htn);
            _faction.ClearFactions(uid, dirty: false);
            _faction.AddFaction(uid, "SimpleHostile");

            if (hasMind && _mindSystem.TryGetSession(mindId, out var session))
            {
                _chatMan.DispatchServerMessage(session, Loc.GetString("Вас поглотило безумие, вы больше не подвластны самому себе."));

                if (mind != null)
                {
                    var position = Deleted(mind.OwnedEntity)
                        ? _gameTicker.GetObserverSpawnPoint().ToMap(EntityManager, _transform)
                        : _transform.GetMapCoordinates(mind.OwnedEntity.Value);



                    var entity = Spawn(GameTicker.ObserverPrototypeName, position);
                    EnsureComp<MindContainerComponent>(entity);
                    var ghostComponent = Comp<GhostComponent>(entity);
                    _ghosts.SetCanReturnToBody(ghostComponent, false);

                    _mindSystem.Visit(mindId, entity, mind);
                    comp.Ghost = entity;
                }
            }

            comp.IsCrazy = true;
        }

        private void UnCrazy(EntityUid uid, SanityComponent comp)
        {
            if (!comp.IsCrazy)
                return;

            _faction.ClearFactions(uid, dirty: false);

            if (!string.IsNullOrEmpty(comp.OldTask))
            {
                RemComp<HTNComponent>(uid);
                var htn = EnsureComp<HTNComponent>(uid);
                htn.RootTask = new HTNCompoundTask() { Task = comp.OldTask };
                htn.Blackboard.SetValue(NPCBlackboard.Owner, uid);
                _npc.WakeNPC(uid, htn);
                _faction.ClearFactions(uid, dirty: false);
                _faction.AddFaction(uid, "SimpleHostile");
                comp.OldTask = "";
            }
            else
            {
                RemComp<HTNComponent>(uid);
            }

            if (comp.OldFaction != null)
                _faction.AddFaction(uid, comp.OldFaction);

            if (!TryComp<GhostComponent>(comp.Ghost, out var ghostComponent))
                return;

            _ghosts.SetCanReturnToBody(ghostComponent, true);

            if (!_mindSystem.TryGetMind(comp.Ghost, out var mindId, out var mind))
                return;

            if (_mindSystem.TryGetSession(mindId, out var session))
                _chatMan.DispatchServerMessage(session, Loc.GetString("Вы можете вернуться в своё тело."));

            comp.IsCrazy = false;
        }
        static ProtoId<NpcFactionPrototype>? GetFirstElement(HashSet<ProtoId<NpcFactionPrototype>> set)
        {
            foreach (var element in set)
            {
                return element;
            }

            return null;
        }
    }
}
