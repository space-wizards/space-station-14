using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Preferences.Managers;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
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
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly HashSet<Entity<StationAiCoreComponent>> _ais = new();

    private ProtoId<JobPrototype> _stationAiJobProto = "JobStationAi";
    private ProtoId<LoadoutGroupPrototype> _stationAiIconography = "StationAiIconography";
    private ProtoId<LoadoutPrototype> _stationAiDefaultIconLoadoutProto = "StationAiIconAi";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandICChatRecipients);
        SubscribeLocalEvent<StationAiHeldComponent, MindAddedMessage>(OnMindAdded);
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
                continue;

            if (stationAiCore.Comp.RemoteEntity == null || stationAiCore.Comp.Remote)
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

    private void OnMindAdded(Entity<StationAiHeldComponent> entity, ref MindAddedMessage ev)
    {
        if (!TryGetStationAiCore((entity.Owner, entity.Comp), out var parentEntity))
            return;

        if (!TryComp<StationAiHolderComponent>(parentEntity, out var stationAiHolder))
            return;
 
        UpdateAppearance((parentEntity.Value, stationAiHolder));
    }

    protected override void UpdateAppearance(Entity<StationAiHolderComponent?> entity)
    {
        base.UpdateAppearance(entity);

        if (!TryComp<StationAiCoreComponent>(entity, out var stationAiCore) || !TryGetInsertedAI((entity, stationAiCore), out var insertedAi))
            return;

        _appearance.SetData(entity.Owner, StationAiIconState.Key, string.Empty);

        if (!_appearance.TryGetData<StationAiState>(entity, StationAiVisualState.Key, out var state) || state == StationAiState.Empty)
            return;

        if (!TryComp<ActorComponent>(insertedAi, out var actor) || actor.PlayerSession.AttachedEntity == null)
            return;

        var prefs = _preferences.GetPreferences(actor.PlayerSession.UserId);
        var profile = prefs.SelectedCharacter as HumanoidCharacterProfile;

        if (profile == null || !profile.Loadouts.TryGetValue(_stationAiJobProto.Id, out var roleLoadout))
            return;

        var loadoutProtoId = _stationAiDefaultIconLoadoutProto;

        if (roleLoadout.SelectedLoadouts.TryGetValue(_stationAiIconography.Id, out var loadout) && loadout.Count > 0)
            loadoutProtoId = loadout[0].Prototype;

        _proto.TryIndex(loadoutProtoId, out var loadoutProto);

        string icon = string.Empty;

        if (loadoutProto?.SpriteLayerData?.TryGetValue(state.ToString(), out var layerData) == true && layerData.State != null)
            icon = layerData.State;

        _appearance.SetData(entity.Owner, StationAiIconState.Key, icon);
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
}
