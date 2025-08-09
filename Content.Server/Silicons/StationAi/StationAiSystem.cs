using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Server.Ghost;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Spawners.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server.Silicons.StationAi;

public sealed class StationAiSystem : SharedStationAiSystem
{
    [Dependency] private readonly IChatManager _chats = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private readonly HashSet<Entity<StationAiCoreComponent>> _ais = new();

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

    protected override void OnAiMapInit(Entity<StationAiCoreComponent> ent, ref MapInitEvent args)
    {
        base.OnAiMapInit(ent, ref args);

        UpdateBatteryAlert(ent);
        UpdateCoreIntegrityAlert(ent);
    }

    protected override void OnAiInsert(Entity<StationAiCoreComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        base.OnAiInsert(ent, ref args);

        UpdateBatteryAlert(ent);
        UpdateCoreIntegrityAlert(ent);
    }

    protected override void OnAiRemove(Entity<StationAiCoreComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        base.OnAiRemove(ent, ref args);

        _alerts.ClearAlert(args.Entity, _batteryAlert);
        _alerts.ClearAlert(args.Entity, _integrityAlert);
    }

    private void OnChargeChanged(Entity<StationAiCoreComponent> entity, ref ChargeChangedEvent args)
    {
        UpdateBatteryAlert(entity);
    }

    private void OnDamageChanged(Entity<StationAiCoreComponent> entity, ref DamageChangedEvent args)
    {
        UpdateCoreIntegrityAlert(entity);
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

    public override bool SetVisionEnabled(Entity<StationAiVisionComponent> entity, bool enabled, bool announce = false)
    {
        if (!base.SetVisionEnabled(entity, enabled, announce))
            return false;

        if (announce)
        {
            AnnounceSnip(entity.Owner);
        }

        return true;
    }

    public override bool SetWhitelistEnabled(Entity<StationAiWhitelistComponent> entity, bool enabled, bool announce = false)
    {
        if (!base.SetWhitelistEnabled(entity, enabled, announce))
            return false;

        if (announce)
        {
            AnnounceSnip(entity.Owner);
        }

        return true;
    }

    public override void AnnounceIntellicardUsage(EntityUid uid, SoundSpecifier? cue = null)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("ai-consciousness-download-warning");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chats.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);

        if (cue != null && _mind.TryGetMind(uid, out var mindId, out _))
            _roles.MindPlaySound(mindId, cue);
    }

    private void AnnounceSnip(EntityUid entity)
    {
        var xform = Transform(entity);

        if (!TryComp(xform.GridUid, out MapGridComponent? grid))
            return;

        _ais.Clear();
        _lookup.GetChildEntities(xform.GridUid.Value, _ais);
        var filter = Filter.Empty();

        foreach (var ai in _ais)
        {
            // TODO: Filter API?
            if (TryComp(ai.Owner, out ActorComponent? actorComp))
            {
                filter.AddPlayer(actorComp.PlayerSession);
            }
        }

        // TEST
        // filter = Filter.Broadcast();

        // No easy way to do chat notif embeds atm.
        var tile = Maps.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
        var msg = Loc.GetString("ai-wire-snipped", ("coords", tile));

        _chats.ChatMessageToMany(ChatChannel.Notifications, msg, msg, entity, false, true, filter.Recipients.Select(o => o.Channel));
        // Apparently there's no sound for this.
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
}
