    using Content.Server.Atmos.Components;
    using Content.Shared._DarkAscent.Temperature.Firebase;
    using Content.Shared.Audio;
    using Robust.Server.GameObjects;
    using Robust.Shared.Timing;

    namespace Content.Server._DarkAscent.Temperature.Firebase;

    public sealed partial class FirebaseSystem : EntitySystem
    {

        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = AllEntityQuery<FirebaseComponent, FlammableComponent>();
            while (query.MoveNext(out var uid, out var firebase, out var flammable))
            {
                if (!flammable.OnFire)
                    continue;

                if (_timing.CurTime <= firebase.NextUpdateTime)
                    continue;

                firebase.NextUpdateTime = _timing.CurTime + firebase.UpdateFrequency;

                BurnFuel(uid, firebase, flammable);
            }
        }

        private void BurnFuel(EntityUid uid, FirebaseComponent firebase, FlammableComponent flammable)
        {
            if (firebase.Fuel <= 0)
            {
                // Decrease firestacks to extinguish the actual flame.
                flammable.FirestackFade -= firebase.FireFadeTick;
                return;
            }

            // Avoid going into negatives.
            firebase.Fuel = Math.Max(firebase.Fuel - firebase.FuelBurntPerUpdate, 0);

            // Keep firestacks stable.
            flammable.FirestackFade = firebase.FireFadeTick;

            UpdateAppearance(uid, firebase, flammable);
        }

        /// <summary>
        /// Changes the appearence according to fuel amount
        /// </summary>
        private void UpdateAppearance(EntityUid uid, FirebaseComponent firebase, FlammableComponent flammable, AppearanceComponent? appearance = null)
        {
            if(!Resolve(uid, ref appearance))
                return;

            if (firebase.Fuel < firebase.FuelBurntPerUpdate)
            {
                _appearance.SetData(uid, FirebaseVisuals.State, FirebaseStatus.Off, appearance);
            }
            else
            {
                _appearance.SetData(uid, FirebaseVisuals.State, FirebaseStatus.On, appearance);
            }

        }
    }
