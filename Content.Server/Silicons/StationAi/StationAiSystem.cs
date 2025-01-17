using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Content.Server.Chat.Systems;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server.Silicons.StationAi;

public sealed class StationAiSystem : SharedStationAiSystem
{
    [Dependency] private readonly IChatManager _chats = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    private readonly Dictionary<EntityUid, HashSet<Entity<StationAiHeldComponent>>> _nais = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
        SubscribeNetworkEvent<AiAlertEvent>(OnAiAlert);
    }

    private void OnExpandICChatRecipients(ExpandICChatRecipientsEvent ev)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = Transform(ev.Source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        // This function ensures that chat popups appear on camera views that have connected microphones.
        var query = EntityManager.EntityQueryEnumerator<StationAiCoreComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var entStationAiCore, out var entXform))
        {
            var stationAiCore = new Entity<StationAiCoreComponent>(ent, entStationAiCore);

            if (!TryGetInsertedAI(stationAiCore, out var insertedAi) || !TryComp(insertedAi, out ActorComponent? actor))
                return;

            if (stationAiCore.Comp.RemoteEntity == null || stationAiCore.Comp.Remote)
                return;

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
            AlertAis(entity.Owner, AiAlertType.AiWireSnipped);
        }

        return true;
    }

    public override bool SetWhitelistEnabled(Entity<StationAiWhitelistComponent> entity, bool enabled, bool announce = false)
    {
        if (!base.SetWhitelistEnabled(entity, enabled, announce))
            return false;

        if (announce)
        {
            AlertAis(entity.Owner, AiAlertType.AiWireSnipped);
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

    private void OnAiAlert(AiAlertEvent ev)
    {
        if(ev.Handled)
            return;
        ev.Handled = true;
        if(!TryGetEntity(ev.target, out var targetUid))
            return;
        
        AlertAis(targetUid.Value, ev.type);
    }

    private void AlertAis(EntityUid target, AiAlertType type)
    {
        switch(type)
        {
            // When lost track, target is AI uid that lost track
            case AiAlertType.LostFollowed:
            case AiAlertType.FollowedFound:
            case AiAlertType.ReFollowingCanceled:
                AlertAiLostFollowed(target, type);
            break;

            case AiAlertType.AiWireSnipped:
                AlertAiWireSnipped(target, type);
            break;

            default:
            break;
        }
    }

    private void AlertAiLostFollowed(EntityUid alertedHeld, AiAlertType type)
    {
        if (!TryComp(alertedHeld, out ActorComponent? aComp))
            return;
        string message;

        switch(type)
        {
            case AiAlertType.LostFollowed:
                message = Loc.GetString("lost-track-message");
            break;
            case AiAlertType.FollowedFound:
                message = Loc.GetString("followed-found-message");
            break;
            case AiAlertType.ReFollowingCanceled:
                message = Loc.GetString("refollowing-canceled-message");
            break;
            default:
            return;
        }
        _chats.ChatMessageToOne(ChatChannel.Notifications,
            message, message, alertedHeld, false, aComp.PlayerSession.Channel);
    }

    private void AlertAiWireSnipped(EntityUid target, AiAlertType type)
    {
        if(type != AiAlertType.AiWireSnipped)
            return;
        // Needed to get target's station and find all AIs on the station
        var tTransform = Transform(target);
        // not on a grid, so no AIs to alert
        if (!TryComp(tTransform.GridUid, out MapGridComponent? grid))
            return;
        
        var gridUid = tTransform.GridUid.Value;
        
        // Check if we already have a station added to dictionary, if not, add it
        if(!_nais.TryGetValue(gridUid, out _))
        {
            _nais.Add(gridUid, new());
        }
        else
        {
            _nais[gridUid].Clear();
        }
        // Fill AI list for a particular station
        // TODO: maybe make some kind of caching
        // Also, can only lookup AI core, but not held, which has actor component, so need to convert
        HashSet<Entity<StationAiCoreComponent>> aiCores = new();
        _lookup.GetChildEntities<StationAiCoreComponent>(gridUid, aiCores);
        // Get cores' helds
        foreach (var core in aiCores)
        {
            // TryGetHeld(core, out var held) would NOT fucking work
            if(!TryGetHeld((core.Owner, core.Comp), out var held))
                continue;
            if(!TryComp<StationAiHeldComponent>(held, out var heldComp))
                continue;
            _nais[gridUid].Add((held, heldComp));
        }
        
        var filter = Filter.Empty();

        foreach (var ai in _nais[gridUid])
        {
            // TODO: Filter API?
            if (TryComp(ai.Owner, out ActorComponent? actorComp))
            {
                filter.AddPlayer(actorComp.PlayerSession);
            }
        }
        var tile = Maps.LocalToTile(gridUid, grid, tTransform.Coordinates);
        var msg = Loc.GetString("ai-wire-snipped", ("coords", tile));
        _chats.ChatMessageToMany(ChatChannel.Notifications, msg, msg, target, false, true, filter.Recipients.Select(o => o.Channel));
    }
}