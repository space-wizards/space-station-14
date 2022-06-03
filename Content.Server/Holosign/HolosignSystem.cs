using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Server.Coordinates.Helpers;
using Robust.Shared.Timing;

namespace Content.Server.Holosign
{
    public sealed class HolosignSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HolosignProjectorComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<HolosignProjectorComponent, ExaminedEvent>(OnExamine);
        }

        private int GetCharges(HolosignProjectorComponent component)
        {
            return component.CurrentCharges + (int) ((_timing.CurTime - component.LastUsed).TotalSeconds / component.RechargeTime.TotalSeconds);
        }

        private void OnExamine(EntityUid uid, HolosignProjectorComponent component, ExaminedEvent args)
        {
            // TODO: This should probably be using an itemstatus
            // TODO: I'm too lazy to do this rn but it's literally copy-paste from emag.
            var timeRemaining = (component.LastUsed + component.RechargeTime * (component.MaxCharges - component.CurrentCharges) - _timing.CurTime).TotalSeconds % component.RechargeTime.TotalSeconds;
            var charges = GetCharges(component);

            args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", charges)));
            if (charges == component.MaxCharges)
            {
                args.PushMarkup(Loc.GetString("emag-max-charges"));
                return;
            }
            args.PushMarkup(Loc.GetString("emag-recharging", ("seconds", Math.Round(timeRemaining))));
        }

        private void OnUse(EntityUid uid, HolosignProjectorComponent component, UseInHandEvent args)
        {
            if (component.CurrentCharges == 0 || args.Handled)
                return;

            // TODO: Too tired to deal
            var holo = EntityManager.SpawnEntity(component.SignProto, Transform(args.User).Coordinates.SnapToGrid(EntityManager));
            Transform(holo).Anchored = true;

            // Don't reset last use time if it's already accumulating.
            if (component.CurrentCharges == component.MaxCharges)
                component.LastUsed = _timing.CurTime;

            component.CurrentCharges--;
            args.Handled = true;
        }
    }
}
