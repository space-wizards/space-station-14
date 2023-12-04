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
        if (state != null)
            sprite.LayerSetState(ToiletVisualLayers.Door, state);
    }
}

public enum ToiletVisualLayers
{
    Door
}
