using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Content.Server.SurveillanceCamera;

namespace Content.Server.Silicons.StationAi;

public sealed class StationAiSystem : SharedStationAiSystem
{
    [Dependency] private readonly IChatManager _chats = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private readonly HashSet<Entity<StationAiCoreComponent>> _ais = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurveillanceCameraComponent, SurveillanceCameraActivateEvent>(OnSurveillanceCameraActivate);
        SubscribeLocalEvent<SurveillanceCameraComponent, SurveillanceCameraDeactivateEvent>(OnSurveillanceCameraDeactivate);
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

    private void OnSurveillanceCameraActivate(EntityUid camera, SurveillanceCameraComponent component, ref SurveillanceCameraActivateEvent args)
    {
        if (TryComp(args.Camera, out StationAiVisionComponent? aiVision))
            SetVisionEnabled((camera, aiVision), true);
    }

    private void OnSurveillanceCameraDeactivate(EntityUid camera, SurveillanceCameraComponent component, ref SurveillanceCameraDeactivateEvent args)
    {
        if (TryComp(args.Camera, out StationAiVisionComponent? aiVision))
            SetVisionEnabled((camera, aiVision), false);
    }

}
