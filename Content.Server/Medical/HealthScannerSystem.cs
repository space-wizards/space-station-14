using Content.Server.Medical.Components;
using Content.Shared.ActionBlocker;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.MobState.Components;

using static Content.Shared.HealthScanner.SharedHealthScannerComponent;

namespace Content.Server.Medical
{
    [UsedImplicitly]
    internal sealed class HealthScannerSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthScannerComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<HealthScannerComponent, ActivateInWorldEvent>(HandleActivateInWorld);
            SubscribeLocalEvent<HealthScannerComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnComponentInit(EntityUid uid, HealthScannerComponent healthScanner, ComponentInit args)
        {
            base.Initialize();

            healthScanner.UserInterface?.SetState(EmptyUIState);
        }

        private void HandleActivateInWorld(EntityUid uid, HealthScannerComponent healthScanner, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out ActorComponent? actor))
            {
                return;
            }

            healthScanner.UserInterface?.Open(actor.PlayerSession);
        }

        private void OnAfterInteract(EntityUid uid, HealthScannerComponent healthScanner, AfterInteractEvent args)
        {
            UpdateUserInterface(args.Target, healthScanner);
        }

        public static readonly HealthScannerBoundUserInterfaceState EmptyUIState =
            new(
                null,
                null,
                null);

        public HealthScannerBoundUserInterfaceState GetUserInterfaceState(EntityUid? target, HealthScannerComponent scannerComponent)
        {

            if (target == null)
                return EmptyUIState;

            if (!TryComp<DamageableComponent>(target, out var damageable))
                return EmptyUIState;

            var targetName = "Unknown";
            if (TryComp<MetaDataComponent>(target, out var meta))
            {
              targetName = meta.EntityName;
            }

            var totalDamage = 0;
            totalDamage = totalDamage = damageable.TotalDamage.Int();

            var isAlive = false;
            if (TryComp<MobStateComponent>(target, out var mobState))
            {
                isAlive = mobState.IsAlive();
            }

            return new HealthScannerBoundUserInterfaceState(targetName, isAlive,  damageable);
        }

        private void UpdateUserInterface(EntityUid? target, HealthScannerComponent healthScanner)
        {
            if (target == null)
                return;

            var newState = GetUserInterfaceState(target, healthScanner);
            healthScanner.UserInterface?.SetState(newState);
        }
    }
}
