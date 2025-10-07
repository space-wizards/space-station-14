using System.Diagnostics.CodeAnalysis;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Systems;
using Prometheus;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Systems
{
    /// <summary>
    ///     Handles NPCs running every tick.
    /// </summary>
    public sealed partial class NPCSystem : EntitySystem
    {
        private static readonly Gauge ActiveGauge = Metrics.CreateGauge(
            "npc_active_count",
            "Amount of NPCs that are actively processing");

        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly HTNSystem _htn = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

        /// <summary>
        /// Whether any NPCs are allowed to run at all.
        /// </summary>
        public bool Enabled { get; set; } = true;

        private int _maxUpdates;

        private int _count;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            Subs.CVar(_configurationManager, CCVars.NPCEnabled, value => Enabled = value, true);
            Subs.CVar(_configurationManager, CCVars.NPCMaxUpdates, obj => _maxUpdates = obj, true);
        }

        public void OnPlayerNPCAttach(EntityUid uid, HTNComponent component, PlayerAttachedEvent args)
        {
            SleepNPC(uid, NPCSleepingCategories.PlayerAttach, component);
        }

        public void OnPlayerNPCDetach(EntityUid uid, HTNComponent component, PlayerDetachedEvent args)
        {
            if (_mobState.IsIncapacitated(uid) || TerminatingOrDeleted(uid))
                return;

            // This NPC has an attached mind, so it should not wake up.
            if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
                return;

            WakeNPC(uid, NPCSleepingCategories.PlayerAttach, component);
        }

        public void OnNPCMapInit(EntityUid uid, HTNComponent component, MapInitEvent args)
        {
            component.Blackboard.SetValue(NPCBlackboard.Owner, uid);
            WakeNPC(uid, null, component);
        }

        public void OnNPCShutdown(EntityUid uid, HTNComponent component, ComponentShutdown args)
        {
            SleepNPC(uid, null, component);
        }

        /// <summary>
        /// Is the NPC awake and updating?
        /// </summary>
        public bool IsAwake(EntityUid uid, HTNComponent component, ActiveNPCComponent? active = null)
        {
            return Resolve(uid, ref active, false);
        }

        public bool TryGetNpc(EntityUid uid, [NotNullWhen(true)] out NPCComponent? component)
        {
            // If you add your own NPC components then add them here.

            if (TryComp<HTNComponent>(uid, out var htn))
            {
                component = htn;
                return true;
            }

            component = null;
            return false;
        }

        /// <summary>
        /// Allows the NPC to actively be updated. If no category is provided, will always wake up the NPC and will
        /// remove all sleeping categories.
        /// </summary>
        public void WakeNPC(EntityUid uid, NPCSleepingCategories? category = NPCSleepingCategories.Default, HTNComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
            {
                return;
            }

            if (category == null)
                RemComp<NPCSleepingComponent>(uid);
            else if (TryComp<NPCSleepingComponent>(uid, out var sleepingComp))
            {
                sleepingComp.SleepReferences.Remove(category.Value);
                if (sleepingComp.SleepReferences.Count != 0)
                    return;
            }

            RemComp<NPCSleepingComponent>(uid);

            Log.Debug($"Waking {ToPrettyString(uid)}");
            EnsureComp<ActiveNPCComponent>(uid);
        }

        /// <summary>
        /// Sleep the given NPC. If no category is provided, will remove all sleeping information and just sleep the NPC
        /// without restriction.
        /// </summary>
        public void SleepNPC(EntityUid uid, NPCSleepingCategories? category = NPCSleepingCategories.Default, HTNComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
            {
                return;
            }
            if (category == null)
                RemComp<NPCSleepingComponent>(uid);
            else
            {
                EnsureComp<NPCSleepingComponent>(uid, out var sleepingComp);
                if (category != null)
                    sleepingComp.SleepReferences.Add(category.Value);
            }

            // Don't bother with an event
            if (TryComp<HTNComponent>(uid, out var htn))
            {
                if (htn.Plan != null)
                {
                    var currentOperator = htn.Plan.CurrentOperator;
                    _htn.ShutdownTask(currentOperator, htn.Blackboard, HTNOperatorStatus.Failed);
                    _htn.ShutdownPlan(htn);
                    htn.Plan = null;
                }
            }

            Log.Debug($"Sleeping {ToPrettyString(uid)}");
            RemComp<ActiveNPCComponent>(uid);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Enabled)
                return;

            // Add your system here.
            _htn.UpdateNPC(ref _count, _maxUpdates, frameTime);

            ActiveGauge.Set(Count<ActiveNPCComponent>());
        }

        public void OnMobStateChange(EntityUid uid, HTNComponent component, MobStateChangedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            switch (args.NewMobState)
            {
                case MobState.Alive:
                    WakeNPC(uid, NPCSleepingCategories.MobState, component);
                    break;
                case MobState.Critical:
                case MobState.Dead:
                    SleepNPC(uid, NPCSleepingCategories.MobState, component);
                    break;
            }
        }
    }
}
