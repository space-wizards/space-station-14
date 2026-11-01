using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Ghost.Components;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Popups;

public sealed partial class PopupSystem : SharedPopupSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void PopupCursor(string? message, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (TryComp(recipient, out ActorComponent? actor))
            RaiseNetworkEvent(new PopupCursorEvent(message, type, Timing.CurTick), actor.PlayerSession);
    }

    public override void PopupCursor(string? message, ICommonSession recipient, PopupType type = PopupType.Small)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        RaiseNetworkEvent(new PopupCursorEvent(message, type, Timing.CurTick), recipient);
    }

    public override void PopupCursor(string? message, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        RaiseNetworkEvent(new PopupCursorEvent(message, type, Timing.CurTick), filter, recordReplay);
    }

    public override void PopupCoordinates(string? message, EntityCoordinates coordinates, PopupType type = PopupType.Small, int predictionKey = 0)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var mapPos = _transform.ToMapCoordinates(coordinates);
        var filter = VoiceRangeFilter(mapPos);
        RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates), predictionKey), filter);
    }

    public override void PopupCoordinates(string? message, EntityCoordinates coordinates, EntityUid? recipient, PopupType type = PopupType.Small, int predictionKey = 0)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (TryComp(recipient, out ActorComponent? actor))
            RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates), predictionKey), actor.PlayerSession);
    }

    public override void PopupCoordinates(string? message, EntityCoordinates coordinates, ICommonSession recipient, PopupType type = PopupType.Small, int predictionKey = 0)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates), predictionKey), recipient);
    }

    public override void PopupCoordinates(string? message, EntityCoordinates coordinates, Filter filter, bool recordReplay, PopupType type = PopupType.Small, int predictionKey = 0)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        RaiseNetworkEvent(new PopupCoordinatesEvent(message, type, Timing.CurTick, GetNetCoordinates(coordinates), predictionKey), RestrictToVoiceRange(filter, _transform.ToMapCoordinates(coordinates)), recordReplay);
    }

    public override void PopupEntity(string? message, EntityUid uid, PopupType type = PopupType.Small)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var filter = VoiceRangeFilter(_transform.GetMapCoordinates(uid));
        RaiseNetworkEvent(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(uid)), filter);
    }

    public override void PopupEntity(string? message, EntityUid uid, EntityUid? recipient, PopupType type = PopupType.Small)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (TryComp(recipient, out ActorComponent? actor))
            RaiseNetworkEvent(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(uid)), actor.PlayerSession);
    }

    public override void PopupEntity(string? message, EntityUid uid, ICommonSession recipient, PopupType type = PopupType.Small)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        RaiseNetworkEvent(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(uid)), recipient);
    }

    public override void PopupEntity(string? message, EntityUid uid, Filter filter, bool recordReplay, PopupType type = PopupType.Small)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        RaiseNetworkEvent(new PopupEntityEvent(message, type, Timing.CurTick, GetNetEntity(uid)), RestrictToVoiceRange(filter, _transform.GetMapCoordinates(uid)), recordReplay);
    }

    private Filter VoiceRangeFilter(MapCoordinates origin)
    {
        return Filter.Empty()
            .AddInRange(origin, SharedChatSystem.VoiceRange, _player, EntityManager)
            .AddWhereAttachedEntity(HasComp<GhostHearingComponent>);
    }

    private Filter RestrictToVoiceRange(Filter filter, MapCoordinates origin)
    {
        var voiceRangeRecipients = VoiceRangeFilter(origin).Recipients;
        return filter.Clone().RemoveWhere(session => !voiceRangeRecipients.Contains(session));
    }
}
