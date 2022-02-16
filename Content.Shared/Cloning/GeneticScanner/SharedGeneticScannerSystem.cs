using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using JetBrains.Annotations;
using Content.Shared.MobState.Components;
using Robust.Shared.Physics;

namespace Content.Shared.Cloning.GeneticScanner
{
    [UsedImplicitly]
    public abstract class SharedGeneticScannerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedGeneticScannerComponent, CanDragDropOnEvent>(OnCanDragDropOn);
        }

        private void OnCanDragDropOn(EntityUid uid, SharedGeneticScannerComponent component, CanDragDropOnEvent args)
        {
            if (args.Handled) return;

            args.CanDrop = CanInsert(component, args.Dragged);
            args.Handled = true;
        }

        public virtual bool CanInsert(SharedGeneticScannerComponent component, EntityUid entity)
        {
            if (!EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored)
                return false;

            if (!EntityManager.HasComponent<SharedBodyComponent>(entity))
            {
                return false;
            }


            // if (!TryComp<IPhysBody>(entity, out IPhysBody? physics) ||
            //     !physics.CanCollide && storable == null)
            // {
            //     if (!(TryComp<MobStateComponent>(entity, out MobStateComponent? damageState) && damageState.IsDead()))
            //     {
            //         return false;
            //     }
            // }

            return true;
        }
    }
}
