using Content.Server.Lathe.Components;
using JetBrains.Annotations;
using Content.Shared.Interaction;
using Content.Server.Materials;
using Content.Server.Stack;
using Robust.Shared.GameObjects;

namespace Content.Server.Lathe
{
    [UsedImplicitly]
    internal sealed class LatheSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LatheComponent, InteractUsingEvent>(OnInteractUsing);
        }
        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<LatheComponent>())
            {
                if (comp.Producing == false && comp.Queue.Count > 0)
                {
                    comp.Produce(comp.Queue.Dequeue());
                }
            }
        }

        private void OnInteractUsing(EntityUid uid, LatheComponent component, InteractUsingEvent args)
        {
            if (!TryComp<MaterialStorageComponent>(uid, out var storage) || !TryComp<MaterialComponent>(args.Used, out var material))
                return;

            var multiplier = 1;

            if (TryComp<StackComponent>(args.Used, out var stack))
                multiplier = stack.Count;

            var totalAmount = 0;

            // Check if it can insert all materials.
            foreach (var mat in material.MaterialIds)
            {
                // TODO: Change how MaterialComponent works so this is not hard-coded.
                if (!storage.CanInsertMaterial(mat, component.VolumePerSheet * multiplier))
                    return;
                totalAmount += component.VolumePerSheet * multiplier;
            }

            // Check if it can take ALL of the material's volume.
            if (storage.StorageLimit != -1 && !storage.CanTakeAmount(totalAmount))
                return;

            foreach (var mat in material.MaterialIds)
            {
                storage.InsertMaterial(mat, component.VolumePerSheet * multiplier);
            }

            EntityManager.QueueDeleteEntity(args.Used);

            args.Handled = true;
        }
    }
}
