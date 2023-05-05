using Content.Server.Administration.Logs;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Electrocution;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Electrocution;

public sealed class ElectrocutionSystem : SharedElectrocutionSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const string StatusEffectKey = "Electrocution";
    private const string DamageType = "Shock";

    // Yes, this is absurdly small for a reason.
    private const float ElectrifiedDamagePerWatt = 0.0015f;

    private const float RecursiveDamageMultiplier = 0.75f;
    private const float RecursiveTimeMultiplier = 0.8f;

    private const float ParalyzeTimeMultiplier = 1f;

    private const float StutteringTimeMultiplier = 1.5f;

    private const float JitterTimeMultiplier = 0.75f;
    private const float JitterAmplitude = 80f;
    private const float JitterFrequency = 8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectrifiedComponent, StartCollideEvent>(OnElectrifiedStartCollide);
        SubscribeLocalEvent<ElectrifiedComponent, AttackedEvent>(OnElectrifiedAttacked);
        SubscribeLocalEvent<ElectrifiedComponent, InteractHandEvent>(OnElectrifiedHandInteract);
        SubscribeLocalEvent<ElectrifiedComponent, InteractUsingEvent>(OnElectrifiedInteractUsing);
        SubscribeLocalEvent<RandomInsulationComponent, MapInitEvent>(OnRandomInsulationMapInit);

        UpdatesAfter.Add(typeof(PowerNetSystem));
    }

    public override void Update(float frameTime)
    {
        UpdateElectrocutions(frameTime);
        UpdateState(frameTime);
    }

    private void UpdateElectrocutions(float frameTime)
    {
        var query = EntityQueryEnumerator<ElectrocutionComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var uid, out var electrocution, out var consumer))
        {
            var timePassed = Math.Min(frameTime, electrocution.TimeLeft);

            electrocution.TimeLeft -= timePassed;
            electrocution.AccumulatedDamage += consumer.ReceivedPower * ElectrifiedDamagePerWatt * timePassed;

            if (!MathHelper.CloseTo(electrocution.TimeLeft, 0))
                continue;

            if (EntityManager.EntityExists(electrocution.Electrocuting))
            {
                // TODO: damage should be scaled by shock damage multiplier
                // TODO: better paralyze/jitter timing
                var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(DamageType), (int) electrocution.AccumulatedDamage);

                var actual = _damageable.TryChangeDamage(electrocution.Electrocuting, damage, origin: electrocution.Source);
                if (actual != null)
                {
                    _adminLogger.Add(LogType.Electrocution,
                        $"{ToPrettyString(electrocution.Electrocuting):entity} received {actual.Total:damage} powered electrocution damage from {ToPrettyString(electrocution.Source):source}");
                }
            }
            QueueDel(uid);
        }
    }

    private void UpdateState(float frameTime)
    {
        var query = EntityQueryEnumerator<ActivatedElectrifiedComponent, ElectrifiedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var activated, out var electrified, out var transform))
        {
            activated.TimeLeft -= frameTime;
            if (activated.TimeLeft <= 0 || !IsPowered(uid, electrified, transform))
            {
                _appearance.SetData(uid, ElectrifiedVisuals.IsPowered, false);
                RemComp<ActivatedElectrifiedComponent>(uid);
            }
        }
    }

    private bool IsPowered(EntityUid uid, ElectrifiedComponent electrified, TransformComponent transform)
    {
        if (!electrified.Enabled)
            return false;
        if (electrified.NoWindowInTile)
        {
            foreach (var entity in transform.Coordinates.GetEntitiesInTile(LookupFlags.Approximate | LookupFlags.Static, _entityLookup))
            {
                if (_tag.HasTag(entity, "Window"))
                    return false;
            }
        }
        if (electrified.UsesApcPower)
        {
            if (!this.IsPowered(uid, EntityManager))
                return false;
        }
        else if (electrified.RequirePower && PoweredNode(uid, electrified) == null)
            return false;

        return true;
    }

    private void OnElectrifiedStartCollide(EntityUid uid, ElectrifiedComponent electrified, ref StartCollideEvent args)
    {
        if (electrified.OnBump)
            TryDoElectrifiedAct(uid, args.OtherFixture.Body.Owner, 1, electrified);
    }

        private void OnElectrifiedAttacked(EntityUid uid, ElectrifiedComponent electrified, AttackedEvent args)
        {
            if (!electrified.OnAttacked)
                return;

            //Dont shock if the attacker used a toy
            if (EntityManager.TryGetComponent<MeleeWeaponComponent>(args.Used, out var meleeWeaponComponent))
            {
                if (meleeWeaponComponent.Damage.Total == 0)
                    return;
            }

            TryDoElectrifiedAct(uid, args.User, 1, electrified);
    }

    private void OnElectrifiedHandInteract(EntityUid uid, ElectrifiedComponent electrified, InteractHandEvent args)
    {
        if (electrified.OnHandInteract)
            TryDoElectrifiedAct(uid, args.User, 1, electrified);
    }

    private void OnElectrifiedInteractUsing(EntityUid uid, ElectrifiedComponent electrified, InteractUsingEvent args)
    {
        if (!electrified.OnInteractUsing)
            return;

        var siemens = TryComp<InsulatedComponent>(args.Used, out var insulation)
            ? insulation.SiemensCoefficient
            : 1;

        TryDoElectrifiedAct(uid, args.User, siemens, electrified);
    }

    public bool TryDoElectrifiedAct(EntityUid uid, EntityUid targetUid,
        float siemens = 1,
        ElectrifiedComponent? electrified = null,
        NodeContainerComponent? nodeContainer = null,
        TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref electrified, ref transform, false))
            return false;

        if (!IsPowered(uid, electrified, transform))
            return false;

        EnsureComp<ActivatedElectrifiedComponent>(uid);
        _appearance.SetData(uid, ElectrifiedVisuals.IsPowered, true);

        siemens *= electrified.SiemensCoefficient;
        if (siemens <= 0 || !DoCommonElectrocutionAttempt(targetUid, uid, ref siemens))
            return false; // If electrocution would fail, do nothing.

        var targets = new List<(EntityUid entity, int depth)>();
        GetChainedElectrocutionTargets(targetUid, targets);
        if (!electrified.RequirePower || electrified.UsesApcPower)
        {
            var lastRet = true;
            for (var i = targets.Count - 1; i >= 0; i--)
            {
                var (entity, depth) = targets[i];
                lastRet = TryDoElectrocution(
                    entity,
                    uid,
                    (int) (electrified.ShockDamage * MathF.Pow(RecursiveDamageMultiplier, depth)),
                    TimeSpan.FromSeconds(electrified.ShockTime * MathF.Pow(RecursiveTimeMultiplier, depth)), 
                    true,
                    electrified.SiemensCoefficient
                );
            }
            return lastRet;
        }

        var node = PoweredNode(uid, electrified, nodeContainer);
        if (node == null)
            return false;

        var (damageMult, timeMult) = node.NodeGroupID switch
        {
            NodeGroupID.HVPower => (electrified.HighVoltageDamageMultiplier, electrified.HighVoltageTimeMultiplier),
            NodeGroupID.MVPower => (electrified.MediumVoltageDamageMultiplier, electrified.MediumVoltageTimeMultiplier),
            _ => (1f, 1f)
        };

        {
            var lastRet = true;
            for (var i = targets.Count - 1; i >= 0; i--)
            {
                var (entity, depth) = targets[i];
                lastRet = TryDoElectrocutionPowered(
                    entity,
                    uid,
                    node,
                    (int) (electrified.ShockDamage * MathF.Pow(RecursiveDamageMultiplier, depth) * damageMult),
                    TimeSpan.FromSeconds(electrified.ShockTime * MathF.Pow(RecursiveTimeMultiplier, depth) * timeMult), 
                    true,
                    electrified.SiemensCoefficient);
            }
            return lastRet;
        }
    }

    private Node? PoweredNode(EntityUid uid, ElectrifiedComponent electrified, NodeContainerComponent? nodeContainer = null)
    {
        if (!Resolve(uid, ref nodeContainer, false))
            return null;

        return TryNode(electrified.HighVoltageNode) ?? TryNode(electrified.MediumVoltageNode) ?? TryNode(electrified.LowVoltageNode);

        Node? TryNode(string? id)
        {
            if (id != null &&
                nodeContainer.TryGetNode<Node>(id, out var tryNode) &&
                tryNode.NodeGroup is IBasePowerNet { NetworkNode: { LastCombinedSupply: > 0 } })
            {
                return tryNode;
            }
            return null;
        }
    }

        /// <param name="uid">Entity being electrocuted.</param>
        /// <param name="sourceUid">Source entity of the electrocution.</param>
        /// <param name="shockDamage">How much shock damage the entity takes.</param>
        /// <param name="time">How long the entity will be stunned.</param>
        /// <param name="refresh">Should <paramref>time</paramref> be refreshed (instead of accumilated) if the entity is already electrocuted?</param>
        /// <param name="siemensCoefficient">How insulated the entity is from the shock. 0 means completely insulated, and 1 means no insulation.</param>
        /// <param name="statusEffects">Status effects to apply to the entity.</param>
        /// <param name="ignoreInsulation">Should the electrocution bypass the Insulated component?</param>
        /// <returns>Whether the entity <see cref="uid"/> was stunned by the shock.</returns>
        public bool TryDoElectrocution(
            EntityUid uid, EntityUid? sourceUid, int shockDamage, TimeSpan time, bool refresh, float siemensCoefficient = 1f,
            StatusEffectsComponent? statusEffects = null, bool ignoreInsulation = false)
        {
            if (!DoCommonElectrocutionAttempt(uid, sourceUid, ref siemensCoefficient, ignoreInsulation)
                || !DoCommonElectrocution(uid, sourceUid, shockDamage, time, refresh, siemensCoefficient, statusEffects))
                return false;

            RaiseLocalEvent(uid, new ElectrocutedEvent(uid, sourceUid, siemensCoefficient), true);
            return true;
        }

    private bool TryDoElectrocutionPowered(
        EntityUid uid,
        EntityUid sourceUid,
        Node node,
        int shockDamage,
        TimeSpan time,
        bool refresh,
        float siemensCoefficient = 1f,
        StatusEffectsComponent? statusEffects = null,
        TransformComponent? sourceTransform = null)
    {
        if (!DoCommonElectrocutionAttempt(uid, sourceUid, ref siemensCoefficient))
            return false;

        // Coefficient needs to be higher than this to do a powered electrocution!
        if (siemensCoefficient <= 0.5f)
            return DoCommonElectrocution(uid, sourceUid, shockDamage, time, refresh, siemensCoefficient, statusEffects);

        if (!DoCommonElectrocution(uid, sourceUid, null, time, refresh, siemensCoefficient, statusEffects))
            return false;

        if (!Resolve(sourceUid, ref sourceTransform)) // This shouldn't really happen, but just in case...
            return true;

        var electrocutionEntity = Spawn($"VirtualElectrocutionLoad{node.NodeGroupID}", sourceTransform.Coordinates);
        var electrocutionNode = Comp<NodeContainerComponent>(electrocutionEntity).GetNode<ElectrocutionNode>("electrocution");
        var electrocutionComponent = Comp<ElectrocutionComponent>(electrocutionEntity);

        electrocutionNode.CableEntity = sourceUid;
        electrocutionNode.NodeName = node.Name;

        _nodeGroup.QueueReflood(electrocutionNode);

        electrocutionComponent.TimeLeft = 1f;
        electrocutionComponent.Electrocuting = uid;
        electrocutionComponent.Source = sourceUid;

        RaiseLocalEvent(uid, new ElectrocutedEvent(uid, sourceUid, siemensCoefficient), true);

        return true;
    }

    private bool DoCommonElectrocutionAttempt(EntityUid uid, EntityUid? sourceUid, ref float siemensCoefficient, bool ignoreInsulation = false)
    {

        var attemptEvent = new ElectrocutionAttemptEvent(uid, sourceUid, siemensCoefficient,
            ignoreInsulation ? SlotFlags.NONE : ~SlotFlags.POCKET);
        RaiseLocalEvent(uid, attemptEvent, true);

        // Cancel the electrocution early, so we don't recursively electrocute anything.
        if (attemptEvent.Cancelled)
            return false;

        siemensCoefficient = attemptEvent.SiemensCoefficient;
        return true;
    }

    private bool DoCommonElectrocution(EntityUid uid, EntityUid? sourceUid,
        int? shockDamage, TimeSpan time, bool refresh, float siemensCoefficient = 1f,
        StatusEffectsComponent? statusEffects = null)
    {
        if (siemensCoefficient <= 0)
            return false;

        if (shockDamage != null)
        {
            shockDamage = (int) (shockDamage * siemensCoefficient);

            if (shockDamage.Value <= 0)
                return false;
        }

        if (!Resolve(uid, ref statusEffects, false) ||
            !_statusEffects.CanApplyEffect(uid, StatusEffectKey, statusEffects))
        {
            return false;
        }
        
        if (!_statusEffects.TryAddStatusEffect<ElectrocutedComponent>(uid, StatusEffectKey, time, refresh, statusEffects))
            return false;

        var shouldStun = siemensCoefficient > 0.5f;

        if (shouldStun)
            _stun.TryParalyze(uid, time * ParalyzeTimeMultiplier, refresh, statusEffects);

        // TODO: Sparks here.

        if (shockDamage is { } dmg)
        {
            var actual = _damageable.TryChangeDamage(uid,
                new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(DamageType), dmg), origin: sourceUid);

            if (actual != null)
            {
                _adminLogger.Add(LogType.Electrocution,
                    $"{ToPrettyString(uid):entity} received {actual.Total:damage} powered electrocution damage{(sourceUid != null ? " from " + ToPrettyString(sourceUid.Value) : ""):source}");
            }
        }

        _stuttering.DoStutter(uid, time * StutteringTimeMultiplier, refresh, statusEffects);
        _jittering.DoJitter(uid, time * JitterTimeMultiplier, refresh, JitterAmplitude, JitterFrequency, true, statusEffects);

        _popup.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-popup-player"), uid, uid);

        var filter = Filter.PvsExcept(uid, entityManager: EntityManager);

        // TODO: Allow being able to pass EntityUid to Loc...
        if (sourceUid != null)
        {
            _popup.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-by-source-popup-others",
                ("mob", uid), ("source", (sourceUid.Value))), uid, filter, true);
            PlayElectrocutionSound(uid, sourceUid.Value);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-popup-others",
                ("mob", uid)), uid, filter, true);
        }

        return true;
    }

    private void GetChainedElectrocutionTargets(EntityUid source, List<(EntityUid entity, int depth)> all)
    {
        var visited = new HashSet<EntityUid>();

        GetChainedElectrocutionTargetsRecurse(source, 1, visited, all);
    }

    private void GetChainedElectrocutionTargetsRecurse(
        EntityUid entity,
        int depth,
        HashSet<EntityUid> visited,
        List<(EntityUid entity, int depth)> all)
    {
        all.Add((entity, depth));
        visited.Add(entity);

        if (TryComp<SharedPullableComponent>(entity, out var pullable) &&
            pullable.Puller is { Valid: true } pullerId &&
            !visited.Contains(pullerId))
        {
            GetChainedElectrocutionTargetsRecurse(pullerId, depth + 1, visited, all);
        }

        if (TryComp<SharedPullerComponent>(entity, out var puller) &&
            puller.Pulling is { Valid: true } pullingId &&
            !visited.Contains(pullingId))
        {
            GetChainedElectrocutionTargetsRecurse(pullingId, depth + 1, visited, all);
        }
    }

    private void OnRandomInsulationMapInit(EntityUid uid, RandomInsulationComponent randomInsulation,
        MapInitEvent args)
    {
        if (!TryComp<InsulatedComponent>(uid, out var insulated))
            return;

        if (randomInsulation.List.Length == 0)
            return;

        SetInsulatedSiemensCoefficient(uid, _random.Pick(randomInsulation.List), insulated);
    }

    private void PlayElectrocutionSound(EntityUid targetUid, EntityUid sourceUid, ElectrifiedComponent? electrified = null)
    {
        if (!Resolve(sourceUid, ref electrified) || !electrified.PlaySoundOnShock)
        {
            return;
        }
        _audio.PlayPvs(electrified.ShockNoises, targetUid, AudioParams.Default.WithVolume(electrified.ShockVolume));
    }
}
