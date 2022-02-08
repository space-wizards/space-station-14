using Content.Shared.Body.Components;
using Content.Shared.DragDrop;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.GeneticScanner
{
    public abstract class SharedGeneticScannerComponent : Component, IDragDropOn
    {
        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!IoCManager.Resolve<IEntityManager>().HasComponent<SharedBodyComponent>(eventArgs.Dragged))
            {
                return false;
            }

            return true;
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);

        [Serializable, NetSerializable]
        public enum GeneticScannerVisuals
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum GeneticScannerStatus
        {
            Off,
            Open,
            Red,
            Death,
            Green,
            Yellow,
        }

        public bool CanInsert(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().HasComponent<SharedBodyComponent>(entity);
        }
    }
}
