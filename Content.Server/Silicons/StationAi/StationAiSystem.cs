using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DoAfter;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Speech.Components;
using Content.Shared.StationAi;
using Content.Shared.Turrets;
using Content.Shared.Weapons.Ranged.Events;
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
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private readonly HashSet<Entity<StationAiCoreComponent>> _stationAiCores = new();

    private readonly ProtoId<ChatNotificationPrototype> _turretIsAttackingChatNotificationPrototype = "TurretIsAttacking";
    private readonly ProtoId<ChatNotificationPrototype> _aiWireSnippedChatNotificationPrototype = "AiWireSnipped";

    private readonly ProtoId<AlertPrototype> _batteryAlert = "BorgBattery";
    private readonly ProtoId<AlertPrototype> _integrityAlert = "BorgHealth";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCoreComponent, ContainerSpawnEvent>(OnContainerSpawn);
        SubscribeLocalEvent<StationAiCoreComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<StationAiCoreComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<StationAiCoreComponent, DoAfterAttemptEvent<IntellicardDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
        SubscribeLocalEvent<StationAiTurretComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private void OnContainerSpawn(Entity<StationAiCoreComponent> ent, ref ContainerSpawnEvent args)
    {
        // Ensure that players that recently joined the round will spawn
        // into an AI core that a full battery and full integrity.
        if (TryComp<BatteryComponent>(ent, out var battery))
        {
            _battery.SetCharge(ent, battery.MaxCharge);
        }

        if (TryComp<DamageableComponent>(ent, out var damageable))
        {
            _damageable.SetAllDamage(ent, damageable, 0);
        }
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
        _alerts.ClearAlert(args.Entity, _integrityAlert);

        if (TryComp<DamagedSiliconAccentComponent>(args.Entity, out var accent))
        {
            accent.OverrideChargeLevel = null;
            accent.OverrideTotalDamage = null;
            accent.DamageAtMaxCorruption = null;
        }
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

    private void UpdateDamagedAccent(Entity<StationAiCoreComponent> ent)
    {
        if (!TryGetHeld((ent.Owner, ent.Comp), out var held))
            return;

        if (!TryComp<DamagedSiliconAccentComponent>(held, out var accent))
            return;

        if (TryComp<BatteryComponent>(ent, out var battery))
            accent.OverrideChargeLevel = battery.CurrentCharge / battery.MaxCharge;

        if (TryComp<DamageableComponent>(ent, out var damageable))
            accent.OverrideTotalDamage = damageable.TotalDamage;

        if (TryComp<DestructibleComponent>(ent, out var destructible))
            accent.DamageAtMaxCorruption = _destructible.DestroyedAt(ent, destructible);

        Dirty(held, accent);
    }

    private void UpdateBatteryAlert(Entity<StationAiCoreComponent> ent)
    {
        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        if (!TryGetHeld((ent.Owner, ent.Comp), out var held))
            return;

        var chargePercent = (short)MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);
        _alerts.ShowAlert(held, _batteryAlert, chargePercent);
    }

    private void UpdateCoreIntegrityAlert(Entity<StationAiCoreComponent> ent)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageable))
            return;

        if (!TryComp<DestructibleComponent>(ent, out var destructible))
            return;

        if (!TryGetHeld((ent.Owner, ent.Comp), out var held))
            return;

        var damagePercent = (short)MathF.Round(damageable.TotalDamage.Float() / _destructible.DestroyedAt(ent, destructible).Float() * 4f);
        _alerts.ShowAlert(held, _integrityAlert, damagePercent);
    }

    private void OnDoAfterAttempt(Entity<StationAiCoreComponent> ent, ref DoAfterAttemptEvent<IntellicardDoAfterEvent> args)
    {
        // Do not allow an AI to be uploaded into a currently unpowered or broken AI core.

        if (TryComp<ApcPowerReceiverComponent>(ent, out var apcPower) && !apcPower.Powered)
        {
            args.Cancel();
            return;
        }

        if (TryComp<DestructibleComponent>(ent, out var destructible) && destructible.IsBroken)
        {
            args.Cancel();
            return;
        }
    }

    public override void KillHeldAi(Entity<StationAiCoreComponent> ent)
    {
        base.KillHeldAi(ent);

        if (!TryGetHeld((ent.Owner, ent.Comp), out var held))
            return;

        if (!_mind.TryGetMind(held, out var mindId, out var mind))
            return;

        _ghost.OnGhostAttempt(mindId, false, mind: mind);
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

            hashSet.Add(insertedAi);
        }

        return hashSet;
    }
}
