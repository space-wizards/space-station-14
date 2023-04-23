using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Robust.Shared.Utility;
using Content.Server.Chat.Systems;
using Content.Server.Station.Systems;
using Robust.Shared.Timing;

namespace Content.Server.PowerSink
{
    public sealed class PowerSinkSystem : EntitySystem
    {
        /// <summary>
        /// Percentage of battery full to trigger the announcement warning at.
        /// </summary>
        private const float WarningMessageThreshold = 0.85f;

        private readonly float[] WarningSoundThresholds = new float[] { .90f, .95f, .98f };

        /// <summary>
        /// Length of time to delay explosion from battery full state -- this is used to play
        /// a brief SFX winding up the explosion.
        /// </summary>
        /// <returns></returns>
        private readonly TimeSpan ExplosionDelayTime = new TimeSpan(0, 0, 0, 1, 465);

        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PowerSinkComponent, ExaminedEvent>(OnExamine);
        }

        private void OnExamine(EntityUid uid, PowerSinkComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange || !TryComp<PowerConsumerComponent>(uid, out var consumer))
                return;

            var drainAmount = (int) consumer.NetworkLoad.ReceivingPower / 1000;
            args.PushMarkup(
                Loc.GetString(
                    "powersink-examine-drain-amount",
                    ("amount", drainAmount),
                    ("markupDrainColor", "orange"))
            );
        }

        public override void Update(float frameTime)
        {
            var toRemove = new RemQueue<(EntityUid Entity, PowerSinkComponent Sink, BatteryComponent Battery)>();
            var query = AllEntityQuery<PowerSinkComponent, PowerConsumerComponent, BatteryComponent, TransformComponent>();

            // Realistically it's gonna be like <5 per station.
            while (query.MoveNext(out var entity, out var component, out var networkLoad, out var battery, out var transform))
            {
                if (!transform.Anchored) continue;

                battery.CurrentCharge += networkLoad.NetworkLoad.ReceivingPower / 1000;

                var currentBatteryThreshold = battery.CurrentCharge / battery.MaxCharge;

                // Check for warning message threshold
                if (!component.SentImminentExplosionWarningMessage &&
                    currentBatteryThreshold >= WarningMessageThreshold)
                {
                    NotifyStationOfImminentExplosion(entity, component);
                }

                // Check for warning sound threshold
                foreach (var testThreshold in WarningSoundThresholds)
                {
                    if (currentBatteryThreshold >= testThreshold &&
                        testThreshold > component.HighestWarningSoundThreshold)
                    {
                        component.HighestWarningSoundThreshold = currentBatteryThreshold; // Don't re-play in future until next threshold hit
                        _audio.PlayPvs(component.ElectricSound, entity,
                            new Robust.Shared.Audio.AudioParams(1f, 1f, "Master", SharedAudioSystem.DefaultSoundRange * 5, 1, 25, false, 0f) // audible from farther than usual
                        ); // Play SFX
                        break;
                    }
                }

                // Check for explosion
                if (battery.CurrentCharge < battery.MaxCharge) continue;

                if (component.ExplosionTime == null)
                {
                    // Set explosion sequence to start soon
                    component.ExplosionTime = _gameTiming.CurTime.Add(ExplosionDelayTime);

                    // Wind-up SFX
                    _audio.PlayPvs(component.ChargeFireSound, entity); // Play SFX
                } else if (_gameTiming.CurTime >= component.ExplosionTime) {
                    // Explode!
                    toRemove.Add((entity, component, battery));
                }
            }

            foreach (var (entity, component, battery) in toRemove)
            {
                _explosionSystem.QueueExplosion(entity, "PowerSink", 2000f, 4f, 20f, canCreateVacuum: true);
                EntityManager.RemoveComponent(entity, component);
            }
        }

        private void NotifyStationOfImminentExplosion(EntityUid uid, PowerSinkComponent powerSinkComponent)
        {
            if (powerSinkComponent.SentImminentExplosionWarningMessage)
                return;
            powerSinkComponent.SentImminentExplosionWarningMessage = true;

            var stationSystem = _entitySystemManager.GetEntitySystem<StationSystem>();
            var chatSystem = _entitySystemManager.GetEntitySystem<ChatSystem>();

            var station = stationSystem.GetOwningStation(uid);

            if (station == null)
                return;

            chatSystem.DispatchStationAnnouncement(
                station.Value,
                Loc.GetString("powersink-immiment-explosion-announcement"),
                playDefaultSound: true,
                colorOverride: Color.Yellow
            );
        }
    }
}
