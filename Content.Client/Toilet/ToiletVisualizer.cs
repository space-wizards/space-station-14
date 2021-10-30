using Content.Shared.Toilet;
using Robust.Client.GameObjects;

namespace Content.Client.Toilet
{
    public class ToiletVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite)) return;

            if (!component.TryGetData(ToiletVisuals.LidOpen, out bool lidOpen)) lidOpen = false;
            if (!component.TryGetData(ToiletVisuals.SeatUp, out bool seatUp)) seatUp = false;

            var state = string.Format("{0}_toilet_{1}",
                lidOpen ? "open" : "closed",
                seatUp ? "seat_up" : "seat_down");

            sprite.LayerSetState(0, state);
        }
    }
}
