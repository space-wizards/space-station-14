using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Popups;

public sealed partial class PopupSystem : SharedPopupSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void PopupCursor(string? message, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        if (message == null)
            return;

        if (TryComp(recipient, out ActorComponent? actor))
            RaiseNetworkEvent(new PopupCursorEvent(message, type, Timing.CurTick), actor.PlayerSession);
    }

    public override void PopupCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small)
    {
        if (message == null)
            return;

        RaiseNetworkEvent(new PopupCursorEvent(message, type, Timing.CurTick), recipient);
    }

    public override void PopupCursor(string? message, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
    {
        if (message == null)
            return;

        RaiseNetworkEvent(new PopupCursorEvent(message, type, Timing.CurTick), filter, recordReplay);
    }

    public override void PopupCoordinates(string? message, EntityCoordinates coordinates, PopupType type = PopupType.Small, int predictionKey = 0)
    {
        if (message == null)
            return;

        var mapPos = _transform.ToMapCoordinates(coordinates);
        var filter = Filter.Empty().AddPlayersByPvs(mapPos, entManager: EntityManager, playerMan: _player, cfgMan: _cfg);
        RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates), predictionKey), filter);
    }

    public override void PopupCoordinates(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small, int predictionKey = 0)
    {
        if (message == null)
            return;

        if (TryComp(recipient, out ActorComponent? actor))
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates), predictionKey), actor.PlayerSession);
    }

    public override void PopupCoordinates(string? message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small, int predictionKey = 0)
    {
        if (message == null)
            return;

        RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates), predictionKey), recipient);
    }

    public override void PopupCoordinates(string? message, EntityCoordinates coordinates, Filter filter, bool recordReplay, PopupType type = PopupType.Small, int predictionKey = 0)
    {
        if (message == null)
            return;

        RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates), predictionKey), filter, recordReplay);
    }

    public override void PopupEntity(string? message, EntityUid uid, PopupType type = PopupType.Small)
    {
        if (message == null)
            return;

        var filter = Filter.Empty().AddPlayersByPvs(uid, entityManager: EntityManager, playerMan: _player, cfgMan: _cfg);
        RaiseNetworkEvent(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(uid)), filter);
    }

    public override void PopupEntity(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        if (message == null)
            return;

        if (TryComp(recipient, out ActorComponent? actor))
            RaiseNetworkEvent(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(uid)), actor.PlayerSession);
    }

    public override void PopupEntity(string? message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small)
    {
        if (message == null)
            return;

        RaiseNetworkEvent(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(uid)), recipient);
    }

    public override void PopupEntity(string? message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
    {
        if (message == null)
            return;

        RaiseNetworkEvent(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(uid)), filter, recordReplay);
    }
}
