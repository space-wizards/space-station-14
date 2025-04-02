using Content.Shared.Clothing;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Damage
{
    public sealed class SlowOnDamageSystem : EntitySystem
    {
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SlowOnDamageComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<SlowOnDamageComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

            SubscribeLocalEvent<ClothingSlowOnDamageModifierComponent, InventoryRelayedEvent<ModifySlowOnDamageSpeedEvent>>(OnModifySpeed);
            SubscribeLocalEvent<ClothingSlowOnDamageModifierComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<ClothingSlowOnDamageModifierComponent, ClothingGotEquippedEvent>(OnGotEquipped);
            SubscribeLocalEvent<ClothingSlowOnDamageModifierComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);

            SubscribeLocalEvent<IgnoreSlowOnDamageComponent, ComponentStartup>(OnIgnoreStartup);
            SubscribeLocalEvent<IgnoreSlowOnDamageComponent, ComponentShutdown>(OnIgnoreShutdown);
            SubscribeLocalEvent<IgnoreSlowOnDamageComponent, ModifySlowOnDamageSpeedEvent>(OnIgnoreModifySpeed);
        }

        private void OnRefreshMovespeed(EntityUid uid, SlowOnDamageComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (!EntityManager.TryGetComponent<DamageableComponent>(uid, out var damage))
                return;

            if (damage.TotalDamage == FixedPoint2.Zero)
                return;

            // Get closest threshold
            FixedPoint2 closest = FixedPoint2.Zero;
            var total = damage.TotalDamage;
            foreach (var thres in component.SpeedModifierThresholds)
            {
                if (total >= thres.Key && thres.Key > closest)
                    closest = thres.Key;
            }

            if (closest != FixedPoint2.Zero)
            {
                var speed = component.SpeedModifierThresholds[closest];

                var ev = new ModifySlowOnDamageSpeedEvent(speed);
                RaiseLocalEvent(uid, ref ev);
                args.ModifySpeed(ev.Speed, ev.Speed);
            }
        }

        private void OnDamageChanged(EntityUid uid, SlowOnDamageComponent component, DamageChangedEvent args)
        {
            // We -could- only refresh if it crossed a threshold but that would kind of be a lot of duplicated
            // code and this isn't a super hot path anyway since basically only humans have this

            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
        }

        private void OnModifySpeed(Entity<ClothingSlowOnDamageModifierComponent> ent, ref InventoryRelayedEvent<ModifySlowOnDamageSpeedEvent> args)
        {
            var dif = 1 - args.Args.Speed;
            if (dif <= 0)
                return;

            // reduces the slowness modifier by the given coefficient
            args.Args.Speed += dif * ent.Comp.Modifier;
        }

        private void OnExamined(Entity<ClothingSlowOnDamageModifierComponent> ent, ref ExaminedEvent args)
        {
            var msg = Loc.GetString("slow-on-damage-modifier-examine", ("mod", (1 - ent.Comp.Modifier) * 100));
            args.PushMarkup(msg);
        }

        private void OnGotEquipped(Entity<ClothingSlowOnDamageModifierComponent> ent, ref ClothingGotEquippedEvent args)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(args.Wearer);
        }

        private void OnGotUnequipped(Entity<ClothingSlowOnDamageModifierComponent> ent, ref ClothingGotUnequippedEvent args)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(args.Wearer);
        }

        private void OnIgnoreStartup(Entity<IgnoreSlowOnDamageComponent> ent, ref ComponentStartup args)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent);
        }

        private void OnIgnoreShutdown(Entity<IgnoreSlowOnDamageComponent> ent, ref ComponentShutdown args)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent);
        }

        private void OnIgnoreModifySpeed(Entity<IgnoreSlowOnDamageComponent> ent, ref ModifySlowOnDamageSpeedEvent args)
        {
            args.Speed = 1f;
        }
    }

    [ByRefEvent]
    public record struct ModifySlowOnDamageSpeedEvent(float Speed) : IInventoryRelayEvent
    {
        public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    }
}
