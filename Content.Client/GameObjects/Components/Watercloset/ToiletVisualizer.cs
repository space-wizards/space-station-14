#nullable enable
using Content.Shared.GameObjects.Components.Watercloset;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Watercloset
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
