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
            SubscribeLocalEvent<PowerSinkComponent, AnchorStateChangedEvent>(OnAnchorChange);
        }

        private void OnExamine(EntityUid uid, PowerSinkComponent component, ExaminedEvent args)
        {
            var battery = Comp<BatteryComponent>(component.Owner);
            if (!TryComp<PowerSinkComponent>(uid, out var powerSinkComponent))
                return;
            if (args.IsInDetailsRange)
            {
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

        private void OnAnchorChange(EntityUid uid, PowerSinkComponent component, ref AnchorStateChangedEvent args)
        {
            component.IsAnchored = args.Anchored;
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<PowerSinkComponent>())
            {
                if (comp.IsAnchored)
                {
                    if(TryComp<PowerConsumerComponent>(comp.Owner, out var networkLoad) && TryComp<BatteryComponent>(comp.Owner, out var battery))
                    {
                        // Charge rate is multiplied by how much power it can get
                        battery.CurrentCharge += networkLoad.NetworkLoad.ReceivingPower * frameTime;
                        if (battery.CurrentCharge >= battery.MaxCharge)
                        {
                            _explosionSystem.QueueExplosion(comp.Owner, "Default", 5, 1, 5, canCreateVacuum: false);
                            EntityManager.RemoveComponent(comp.Owner, comp);
                        }
                    }
                }
            }
        }
    }
}
