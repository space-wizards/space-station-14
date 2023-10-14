using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Server.Storage.Components;
using Content.Shared.Placeable;
using Robust.Shared.Physics.Components;
using Content.Shared.Inventory;
using Robust.Shared.Containers;

namespace Content.Server.Physics;
public sealed class WeightSystem : EntitySystem
{
    /// <summary>
    /// Recursively calculates the weight of the object, and all its contents, and the contents and its contents...
    /// </summary>
    public float GetEntWeightRecursive(EntityUid uid)
    {
        var totalMass = 0f;
        if (Deleted(uid)) return 0f;

        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            totalMass += physics.Mass;
        }
        //Containers
        if (TryComp<EntityStorageComponent>(uid, out var entityStorage))
        {
            var storage = entityStorage.Contents;
            foreach (var ent in storage.ContainedEntities)
            {
                totalMass += GetEntWeightRecursive(ent);
            }
        }
        //Inventory
        if (TryComp<ContainerManagerComponent>(uid, out var containerManager))
        {
            foreach (var container in containerManager.Containers)
            {
                var storage = container.Value.ContainedEntities;
                foreach (var ent in storage)
                {
                    totalMass += GetEntWeightRecursive(ent);
                }
            }
        }
        return totalMass;
    }
}
