using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Server.Coordinates.Helpers;

namespace Content.Server.Holosign
{
    public sealed class HolosignSystem : EntitySystem
    {

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var projector in EntityQuery<HolosignProjectorComponent>())
            {
                if (projector.CurrentCharges == projector.Charges)
                    continue;

                projector.Accumulator += frameTime;
                if (projector.Accumulator < projector.RechargeTime.TotalSeconds)
                {
                    continue;
                }

                projector.Accumulator = 0;
                projector.CurrentCharges++;
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HolosignProjectorComponent, UseInHandEvent>(OnUse);
            SubscribeLocalEvent<HolosignProjectorComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, HolosignProjectorComponent component, ExaminedEvent args)
        {
            float timeRemaining = ((float) component.RechargeTime.TotalSeconds - component.Accumulator);
            args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", component.CurrentCharges)));
            if (component.CurrentCharges == component.Charges)
            {
                args.PushMarkup(Loc.GetString("emag-max-charges"));
                return;
            }
            args.PushMarkup(Loc.GetString("emag-recharging", ("seconds", Math.Round(timeRemaining))));
        }

        private void OnUse(EntityUid uid, HolosignProjectorComponent component, UseInHandEvent args)
        {
            if (component.CurrentCharges == 0)
                return;

            EntityManager.SpawnEntity(component.SignProto, Transform(args.User).Coordinates.SnapToGrid());
            component.CurrentCharges--;
        }
    }
}
