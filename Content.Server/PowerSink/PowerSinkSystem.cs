using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Server.PowerSink
{
    public sealed class PowerSinkSystem : EntitySystem
    {
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

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
            var toRemove = new RemQueue<(PowerSinkComponent Sink, BatteryComponent Battery)>();

            // Realistically it's gonna be like <5 per station.
            foreach (var (comp, networkLoad, battery, xform) in EntityManager.EntityQuery<PowerSinkComponent, PowerConsumerComponent, BatteryComponent, TransformComponent>())
            {
                if (!xform.Anchored) continue;

                battery.CurrentCharge += networkLoad.NetworkLoad.ReceivingPower / 1000;
                if (battery.CurrentCharge < battery.MaxCharge) continue;

                toRemove.Add((comp, battery));
            }

            foreach (var (comp, battery) in toRemove)
            {
                _explosionSystem.QueueExplosion(comp.Owner, "Default", 5 * (battery.MaxCharge / 2500000), 0.5f, 10, canCreateVacuum: false);
                EntityManager.RemoveComponent(comp.Owner, comp);
            }
        }
    }
}
