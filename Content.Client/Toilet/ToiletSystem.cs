using Content.Shared.Cabinet;
using Content.Shared.Toilet;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;

namespace Content.Client.Toilet;

public sealed class ToiletSystem : SharedToiletSystem
{
    protected override void UpdateAppearance(EntityUid uid, ToiletComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var state = component.ToggleSeat ? component.SeatUp : component.SeatDown;
        var lidstate = component.ToggleLid ? component.LidOpen : component.LidClosed;
        if (state != null)
            sprite.LayerSetState(ToiletVisualLayers.Door, state);
        if (lidstate != null)
            sprite.LayerSetState(ToiletVisualLayers.Lid, lidstate);
    }
}

public enum ToiletVisualLayers
{
    Door,
    Lid
}
