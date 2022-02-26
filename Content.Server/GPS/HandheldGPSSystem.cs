using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.GPS;

namespace Content.Server.GPS
{
    public sealed class HandheldGPSSystem : EntitySystem
    {
        // Update rate for all GPS to minimize calls
        private const float UpdateRate = 2f;
        private float _updateDif;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandheldGPSComponent, ActivateInWorldEvent>(OnActivateInWorld);
        }

        private void OnActivateInWorld(EntityUid uid, HandheldGPSComponent gpsComp, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<ActorComponent>(args.User, out var actor))
            {
                return;
            }

            gpsComp.UserInterface?.Toggle(actor.PlayerSession);
            UpdateGPSLocation(uid, gpsComp);
        }

        public void UpdateGPSLocation(EntityUid uid, HandheldGPSComponent? gpsComp)
        {
            if (!Resolve(uid, ref gpsComp))
                return;

            TryComp<TransformComponent>(gpsComp.Owner, out TransformComponent? transformComp);
            gpsComp.UserInterface?.SetState(new UpdateGPSLocationState(transformComp?.MapPosition));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // check update rate
            _updateDif += frameTime;
            if (_updateDif < UpdateRate)
                return;
            _updateDif = 0f;

            var GPSList = EntityManager.EntityQuery<HandheldGPSComponent>();
            foreach (var GPS in GPSList)
            {
                UpdateGPSLocation(GPS.Owner, GPS);
            }
        }
    }
}
