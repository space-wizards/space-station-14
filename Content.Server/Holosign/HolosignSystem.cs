using Content.Shared.Interaction.Events;
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
                    return;

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