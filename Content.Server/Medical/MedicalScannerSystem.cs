using Content.Server.Medical.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Medical
{
    [UsedImplicitly]
    internal sealed class MedicalScannerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MedicalScannerComponent, RelayMovementEntityEvent>(OnRelayMovement);
        }

        private void OnRelayMovement(EntityUid uid, MedicalScannerComponent component, RelayMovementEntityEvent args)
        {
            if (_blocker.CanInteract(args.Entity))
            {
                if (_gameTiming.CurTime <
                    component.LastInternalOpenAttempt + MedicalScannerComponent.InternalOpenAttemptDelay)
                {
                    return;
                }

                component.LastInternalOpenAttempt = _gameTiming.CurTime;
                component.EjectBody();
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<MedicalScannerComponent>(true))
            {
                comp.Update(frameTime);
            }
        }
    }
}
