using Content.Shared.Toilet;
using Robust.Client.GameObjects;

namespace Content.Client.Toilet;

public sealed class ToiletVisualsSystem : VisualizerSystem<ToiletComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ToiletComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null) return;

        AppearanceSystem.TryGetData<bool>(uid, ToiletVisuals.LidOpen, out var lidOpen, args.Component);
        AppearanceSystem.TryGetData<bool>(uid, ToiletVisuals.SeatUp, out var seatUp, args.Component);

        var state = (lidOpen, seatUp) switch
        {
            (false, false) => "closed_toilet_seat_down",
            (false, true) => "closed_toilet_seat_up",
            (true, false) => "open_toilet_seat_down",
            (true, true) => "open_toilet_seat_up"
        };

        args.Sprite.LayerSetState(0, state);
    }
}
