using Content.Server.Chat.Systems;
using Content.Server.Construction;
using Content.Server.Destructible;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Power.Components;
using Content.Server.Roles;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Alert;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.Roles;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Speech.Components;
using Content.Shared.StationAi;
using Content.Shared.Turrets;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server.Silicons.StationAi;

public sealed class StationAiSystem : SharedStationAiSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly ToggleableGhostRoleSystem _ghostrole = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private readonly HashSet<Entity<StationAiCoreComponent>> _stationAiCores = new();

    private readonly ProtoId<ChatNotificationPrototype> _turretIsAttackingChatNotificationPrototype = "TurretIsAttacking";
    private readonly ProtoId<ChatNotificationPrototype> _aiWireSnippedChatNotificationPrototype = "AiWireSnipped";
    private readonly ProtoId<ChatNotificationPrototype> _aiLosingPowerChatNotificationPrototype = "AiLosingPower";
    private readonly ProtoId<ChatNotificationPrototype> _aiCriticalPowerChatNotificationPrototype = "AiCriticalPower";

    private readonly ProtoId<JobPrototype> _stationAiJob = "StationAi";
    private readonly EntProtoId _stationAiBrain = "StationAiBrain";

    private readonly ProtoId<AlertPrototype> _batteryAlert = "AiBattery";
    private readonly ProtoId<AlertPrototype> _damageAlert = "BorgHealth";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCoreComponent, AfterConstructionChangeEntityEvent>(AfterConstructionChangeEntity);
        SubscribeLocalEvent<StationAiCoreComponent, ContainerSpawnEvent>(OnContainerSpawn);
        SubscribeLocalEvent<StationAiCoreComponent, ApcPowerReceiverBatteryChangedEvent>(OnApcBatteryChanged);
        SubscribeLocalEvent<StationAiCoreComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<StationAiCoreComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<StationAiCoreComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<StationAiCoreComponent, DoAfterAttemptEvent<IntellicardDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<StationAiCoreComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
        SubscribeLocalEvent<StationAiTurretComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private void AfterConstructionChangeEntity(Entity<StationAiCoreComponent> ent, ref AfterConstructionChangeEntityEvent args)
    {
        if (!_container.TryGetContainer(ent, StationAiCoreComponent.BrainContainer, out var container) ||
            container.Count == 0)
        {
            return;
        }

        var brain = container.ContainedEntities[0];
        var hasMind = _mind.TryGetMind(brain, out var mindId, out var mind);

        if (hasMind || HasComp<GhostRoleComponent>(brain))
        {
            var aiBrain = Spawn(_stationAiBrain, Transform(ent.Owner).Coordinates);

            if (hasMind)
            {
                // Found an existing mind to transfer into the AI core
                _roles.MindAddJobRole(mindId, mind, false, _stationAiJob);
                _mind.TransferTo(mindId, aiBrain);
            }
            else
            {
                // If the brain had a ghost role attached, activate the station AI ghost role
                _ghostrole.ActivateGhostRole(aiBrain);

                // Set the new AI brain to the 'rebooting' state
                if (TryComp<StationAiCustomizationComponent>(aiBrain, out var customization))
                    SetStationAiState((aiBrain, customization), StationAiState.Rebooting);
                
            }

            // Delete the new AI brain if it cannot be inserted into the core
            if (!TryComp<StationAiHolderComponent>(ent, out var targetHolder) ||
                !_slots.TryInsert(ent, targetHolder.Slot, aiBrain, null))
            {
                QueueDel(aiBrain);
            }
        }

        // TODO: We should consider keeping the borg brain inside the AI core.
        // When the core is destroyed, the station AI can be transferred into the brain,
        // then dropped on the ground. The deceased AI can then be revived later,
        // instead of being lost forever.
        QueueDel(brain);
    }

    private void OnContainerSpawn(Entity<StationAiCoreComponent> ent, ref ContainerSpawnEvent args)
    {
        // Ensure that players that recently joined the round will spawn
        // into an AI core that has a full battery and full integrity.
        if (TryComp<BatteryComponent>(ent, out var battery))
        {
            _battery.SetCharge((ent, battery), battery.MaxCharge);
        }

        _damageable.ClearAllDamage(ent.Owner);
    }

    protected override void OnAiInsert(Entity<StationAiCoreComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        base.OnAiInsert(ent, ref args);

        UpdateBatteryAlert(ent);
        UpdateCoreIntegrityAlert(ent);
        UpdateDamagedAccent(ent);
    }

    protected override void OnAiRemove(Entity<StationAiCoreComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        base.OnAiRemove(ent, ref args);

        _alerts.ClearAlert(args.Entity, _batteryAlert);
        _alerts.ClearAlert(args.Entity, _damageAlert);

        if (TryComp<DamagedSiliconAccentComponent>(args.Entity, out var accent))
        {
            accent.OverrideChargeLevel = null;
            accent.OverrideTotalDamage = null;
            accent.DamageAtMaxCorruption = null;
        }
    }

    protected override void OnMobStateChanged(Entity<StationAiCustomizationComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
        {
            SetStationAiState(ent, StationAiState.Dead);
            return;
        }

        var state = StationAiState.Rebooting;

        if (_mind.TryGetMind(ent, out var _, out var mind) && !mind.IsVisitingEntity)
        {
            state = StationAiState.Occupied;
        }

        if (TryGetCore(ent, out var aiCore) && aiCore.Comp != null)
        {
            var aiCoreEnt = (aiCore.Owner, aiCore.Comp);

            if (SetupEye(aiCoreEnt))
                AttachEye(aiCoreEnt);
        }

        SetStationAiState(ent, state);
    }

    private void OnDestruction(Entity<StationAiCoreComponent> ent, ref DestructionEventArgs args)
    {
        var station = _station.GetOwningStation(ent);

        if (station == null)
            return;

        if (!HasComp<ContainerSpawnPointComponent>(ent))
            return;

        // If the destroyed core could act as a player spawn point,
        // reduce the number of available AI jobs by one
        _stationJobs.TryAdjustJobSlot(station.Value, _stationAiJob, -1, false, true);
    }

    private void OnApcBatteryChanged(Entity<StationAiCoreComponent> ent, ref ApcPowerReceiverBatteryChangedEvent args)
    {
        if (!args.Enabled)
            return;

        if (!TryGetHeld((ent.Owner, ent.Comp), out var held))
            return;

        var ev = new ChatNotificationEvent(_aiLosingPowerChatNotificationPrototype, ent);
        RaiseLocalEvent(held.Value, ref ev);
    }

    private void OnChargeChanged(Entity<StationAiCoreComponent> entity, ref ChargeChangedEvent args)
    {
        UpdateBatteryAlert(entity);
        UpdateDamagedAccent(entity);
    }

    private void OnDamageChanged(Entity<StationAiCoreComponent> entity, ref DamageChangedEvent args)
    {
        UpdateCoreIntegrityAlert(entity);
        UpdateDamagedAccent(entity);
    }

    // TODO: This should just read the current damage and charge when speaking instead of updating the component all the time even if we don't even use it.
    private void UpdateDamagedAccent(Entity<StationAiCoreComponent> ent)
    {
        if (!TryGetHeld((ent.Owner, ent.Comp), out var held))
            return;

        if (!TryComp<DamagedSiliconAccentComponent>(held, out var accent))
            return;

        if (TryComp<BatteryComponent>(ent, out var battery))
            accent.OverrideChargeLevel = _battery.GetChargeLevel((ent.Owner, battery));

        if (TryComp<DamageableComponent>(ent, out var damageable))
            accent.OverrideTotalDamage = damageable.TotalDamage;

        if (TryComp<DestructibleComponent>(ent, out var destructible))
            accent.DamageAtMaxCorruption = _destructible.DestroyedAt(ent, destructible);

        Dirty(held.Value, accent);
    }

    private void UpdateBatteryAlert(Entity<StationAiCoreComponent> ent)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        if (!TryGetHeld((ent.Owner, ent.Comp), out var held))
            return;

        if (!_proto.TryIndex(_batteryAlert, out var proto))
            return;

        var chargePercent = _battery.GetChargeLevel((ent.Owner, battery));
        var chargeLevel = Math.Round(chargePercent * proto.MaxSeverity);

        _alerts.ShowAlert(held.Value, _batteryAlert, (short)Math.Clamp(chargeLevel, 0, proto.MaxSeverity));

        if (TryComp<ApcPowerReceiverBatteryComponent>(ent, out var apcBattery) &&
            apcBattery.Enabled &&
            chargePercent < 0.2)
        {
            var ev = new ChatNotificationEvent(_aiCriticalPowerChatNotificationPrototype, ent);
            RaiseLocalEvent(held.Value, ref ev);
        }
    }

    private void UpdateCoreIntegrityAlert(Entity<StationAiCoreComponent> ent)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageable))
            return;

        if (!TryComp<DestructibleComponent>(ent, out var destructible))
            return;

        if (!TryGetHeld((ent.Owner, ent.Comp), out var held))
            return;

        if (!_proto.TryIndex(_damageAlert, out var proto))
            return;

        var damagePercent = damageable.TotalDamage / _destructible.DestroyedAt(ent, destructible);
        var damageLevel = Math.Round(damagePercent.Float() * proto.MaxSeverity);

        _alerts.ShowAlert(held.Value, _damageAlert, (short)Math.Clamp(damageLevel, 0, proto.MaxSeverity));
    }

    private void OnDoAfterAttempt(Entity<StationAiCoreComponent> ent, ref DoAfterAttemptEvent<IntellicardDoAfterEvent> args)
    {
        if (TryGetHeld((ent.Owner, ent.Comp), out _))
            return;

        // Prevent AIs from being uploaded into an unpowered or broken AI core.

        if (TryComp<ApcPowerReceiverComponent>(ent, out var apcPower) && !apcPower.Powered)
        {
            _popups.PopupEntity(Loc.GetString("station-ai-has-no-power-for-upload"), ent, args.Event.User);
            args.Cancel();
        }
        else if (TryComp<DestructibleComponent>(ent, out var destructible) && destructible.IsBroken)
        {
            _popups.PopupEntity(Loc.GetString("station-ai-is-too-damaged-for-upload"), ent, args.Event.User);
            args.Cancel();
        }
    }

    public override void KillHeldAi(Entity<StationAiCoreComponent> ent)
    {
        base.KillHeldAi(ent);

        if (TryGetHeld((ent.Owner, ent.Comp), out var held) &&
            _mind.TryGetMind(held.Value, out var mindId, out var mind))
        {
            _ghost.OnGhostAttempt(mindId, canReturnGlobal: true, mind: mind);
            RemComp<StationAiOverlayComponent>(held.Value);
        }

        ClearEye(ent);
    }

    private void OnRejuvenate(Entity<StationAiCoreComponent> ent, ref RejuvenateEvent args)
    {
        if (TryGetHeld((ent.Owner, ent.Comp), out var held))
        {
            _mobState.ChangeMobState(held.Value, MobState.Alive);
            EnsureComp<StationAiOverlayComponent>(held.Value);
        }

        if (TryComp<StationAiHolderComponent>(ent, out var holder))
        {
            _appearance.SetData(ent, StationAiVisuals.Broken, false);
            UpdateAppearance((ent, holder));
        }
    }

    private void OnExpandICChatRecipients(ExpandICChatRecipientsEvent ev)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = Transform(ev.Source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        // This function ensures that chat popups appear on camera views that have connected microphones.
        var query = EntityQueryEnumerator<StationAiCoreComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entStationAiCore, out var entXform))
        {
            var stationAiCore = new Entity<StationAiCoreComponent?>(ent, entStationAiCore);

            if (!TryGetHeld(stationAiCore, out var insertedAi) || !TryComp(insertedAi, out ActorComponent? actor))
                continue;

            if (stationAiCore.Comp?.RemoteEntity == null || stationAiCore.Comp.Remote)
                continue;

            var xform = Transform(stationAiCore.Comp.RemoteEntity.Value);

            var range = (xform.MapID != sourceXform.MapID)
                ? -1
                : (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

            if (range < 0 || range > ev.VoiceRange)
                continue;

            ev.Recipients.TryAdd(actor.PlayerSession, new ICChatRecipientData(range, false));
        }
    }

    private void OnAmmoShot(Entity<StationAiTurretComponent> ent, ref AmmoShotEvent args)
    {
        var xform = Transform(ent);

        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return;

        var ais = GetStationAIs(xform.GridUid.Value);

        foreach (var ai in ais)
        {
            var ev = new ChatNotificationEvent(_turretIsAttackingChatNotificationPrototype, ent);

            if (TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork))
                ev.SourceNameOverride = Loc.GetString("station-ai-turret-component-name", ("name", Name(ent)), ("address", deviceNetwork.Address));

            RaiseLocalEvent(ai, ref ev);
        }
    }

    public override bool SetVisionEnabled(Entity<StationAiVisionComponent> entity, bool enabled, bool announce = false)
    {
        if (!base.SetVisionEnabled(entity, enabled, announce))
            return false;

        if (announce)
            AnnounceSnip(entity.Owner);

        return true;
    }

    public override bool SetWhitelistEnabled(Entity<StationAiWhitelistComponent> entity, bool enabled, bool announce = false)
    {
        if (!base.SetWhitelistEnabled(entity, enabled, announce))
            return false;

        if (announce)
            AnnounceSnip(entity.Owner);

        return true;
    }

    private void AnnounceSnip(EntityUid uid)
    {
        var xform = Transform(uid);

        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return;

        var ais = GetStationAIs(xform.GridUid.Value);

        foreach (var ai in ais)
        {
            if (!StationAiCanDetectWireSnipping(ai))
                continue;

            var ev = new ChatNotificationEvent(_aiWireSnippedChatNotificationPrototype, uid);

            var tile = Maps.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
            ev.SourceNameOverride = tile.ToString();

            RaiseLocalEvent(ai, ref ev);
        }
    }

    private bool StationAiCanDetectWireSnipping(EntityUid uid)
    {
        // TODO: The ability to detect snipped AI interaction wires
        // should be a MALF ability and/or a purchased upgrade rather
        // than something available to the station AI by default.
        // When these systems are added, add the appropriate checks here.

        return false;
    }

    public HashSet<EntityUid> GetStationAIs(EntityUid gridUid)
    {
        _stationAiCores.Clear();
        _lookup.GetChildEntities(gridUid, _stationAiCores);

        var hashSet = new HashSet<EntityUid>();

        foreach (var stationAiCore in _stationAiCores)
        {
            if (!TryGetHeld((stationAiCore, stationAiCore.Comp), out var insertedAi))
                continue;

            hashSet.Add(insertedAi.Value);
        }

        return hashSet;
    }
}
