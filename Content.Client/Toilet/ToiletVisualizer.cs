using Content.Shared.Toilet;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Toilet
{
    public class ToiletVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite)) return;

            if (!component.TryGetData(ToiletVisuals.LidOpen, out bool lidOpen)) lidOpen = false;
            if (!component.TryGetData(ToiletVisuals.SeatUp, out bool seatUp)) seatUp = false;

            var state = $"{(lidOpen ? "open" : "closed")}_toilet_{(seatUp ? "seat_up" : "seat_down")}";

            sprite.LayerSetState(0, state);
        }
    }
}
