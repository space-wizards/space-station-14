using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Body.Events;
using Content.Shared.Examine;

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
            if (!TryComp<PowerSinkComponent>(uid, out var batteryComponent))
                return;
            if (args.IsInDetailsRange)
            {
                var effectiveMax = component.Capacity;
                if (effectiveMax == 0) {effectiveMax = 1;}
                var chargeFraction = batteryComponent.Charge / effectiveMax;
                var chargePercentRounded = (int) (chargeFraction * 100);
                args.PushMarkup(
                    Loc.GetString(
                        "powersink-examine-charge-percent",
                        ("percent", chargePercentRounded),
                        ("markupPercentColor", "green")
                    )
                );
                var drainAmount = (int) Comp<PowerConsumerComponent>(component.Owner).NetworkLoad.ReceivingPower / 1000;
                args.PushMarkup(
                    Loc.GetString(
                        "powersink-examine-drain-amount",
                        ("amount", drainAmount),
                        ("markupDrainColor", "orange")
                    )
                    );
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<PowerSinkComponent>())
            {
                if (Comp<TransformComponent>(comp.Owner).Anchored)
                {
                    var networkLoad = Comp<PowerConsumerComponent>(comp.Owner).NetworkLoad;
                    // Charge rate is multiplied by how much power it can get
                    comp.Charge += networkLoad.ReceivingPower * 0.000001f * frameTime;
                    if (comp.Charge >= comp.Capacity)
                    {
                        _explosionSystem.QueueExplosion(comp.Owner, "Default", 5, 1, 5, canCreateVacuum: false);
                        comp.AlreadyExploded = true;
                    }
                }
            }
        }
    }
}
