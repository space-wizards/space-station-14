using System;
using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Log;

namespace Content.Shared.GeneticScanner
{
    [UsedImplicitly]
    public abstract class SharedGeneticScannerSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;

        protected static TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedGeneticScannerComponent, CanDragDropOnEvent>(OnCanDragDropOn);
        }

        private void OnCanDragDropOn(EntityUid uid, SharedGeneticScannerComponent component, CanDragDropOnEvent args)
        {
            Logger.Debug("can drop on");
            if (args.Handled) return;

            args.CanDrop = CanInsert(component, args.Dragged);
            args.Handled = true;
        }

        public virtual bool CanInsert(SharedGeneticScannerComponent component, EntityUid entity)
        {
            // if (!EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored)
            //     return false;

            if (!EntityManager.HasComponent<SharedBodyComponent>(entity))
            {
                return false;
            }


            // if (!EntityManager.TryGetComponent(entity, out IPhysBody? physics) ||
            //     !physics.CanCollide && storable == null)
            // {
            //     if (!(EntityManager.TryGetComponent(entity, out MobStateComponent? damageState) && damageState.IsDead()))
            //     {
            //         return false;
            //     }
            // }

            return true;
        }
    }
}
