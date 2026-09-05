using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Medical.BiomassReclaimer
{
    public abstract class SharedBiomassReclaimerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BiomassReclaimerComponent, CanDropTargetEvent>(OnCanDragDrop);
        }

        private void OnCanDragDrop(Entity<BiomassReclaimerComponent> reclaimer, ref CanDropTargetEvent args)
        {
            if (args.Handled)
                return;

            args.CanDrop = HasComp<MobStateComponent>(args.Dragged);
            args.Handled = true;
        }
    }
}
