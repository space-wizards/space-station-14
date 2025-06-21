    using Content.Server.Atmos.Components;
    using Content.Shared.Atmos;
    using Content.Shared.Audio;
    using Robust.Shared.Audio.Systems;
    using Robust.Shared.Timing;

    namespace Content.Server._DarkAscent.Temperature.Firebase;

    public sealed partial class FirebaseSystem : EntitySystem
    {

        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;


        public override void Initialize()
        {
            SubscribeLocalEvent<FlammableComponent, IgnitedEvent>(OnIgnited);
            SubscribeLocalEvent<FlammableComponent, ExtinguishedEvent>(OnExtinguished);
        }

        private void OnIgnited(EntityUid uid, FlammableComponent comp, IgnitedEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out AmbientSoundComponent ?ambient))
            {
                _ambient.SetAmbience(uid, true, ambient);
            }
        }

        private void OnExtinguished(EntityUid uid, FlammableComponent comp, ExtinguishedEvent args)
        {
            if (EntityManager.TryGetComponent(uid, out AmbientSoundComponent ?ambient))
            {
                _ambient.SetAmbience(uid, false, ambient);
            }
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
        }
    }
