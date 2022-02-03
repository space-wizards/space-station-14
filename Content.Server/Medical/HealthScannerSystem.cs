using Content.Server.Medical.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Content.Shared.HealthScanner;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.Weapons.Melee;

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
            SubscribeLocalEvent<HealthScannerComponent, ClickAttackEvent>(OnClickAttack);
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

        private void OnClickAttack(EntityUid uid, HealthScannerComponent healthScanner, ClickAttackEvent args)
        {
            UpdateUserInterface(args.Target, healthScanner);
        }

        public static readonly HealthScannerBoundUserInterfaceState EmptyUIState =
            new(
                null,
                null);

        public HealthScannerBoundUserInterfaceState GetUserInterfaceState(EntityUid? target, HealthScannerComponent scannerComponent)
        {
            if (!TryComp<DamageableComponent>(target, out DamageableComponent? damageable))
            {
                return EmptyUIState;
            }

            return new HealthScannerBoundUserInterfaceState(target, damageable);
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
