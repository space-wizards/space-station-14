using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components;
using Robust.Shared.Containers;

namespace Content.Server.AI.Utility.Considerations.Containers
{
    /// <summary>
    /// Returns 1.0f if the item is freely accessible (e.g. in storage we can open, on ground, etc.)
    /// </summary>
    public sealed class TargetAccessibleCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();
            if (target == null)
            {
                return 0.0f;
            }

            if (ContainerHelpers.TryGetContainer(target, out var container))
            {
                if (container.Owner.TryGetComponent(out EntityStorageComponent storageComponent))
                {
                    if (storageComponent.IsWeldedShut && !storageComponent.Open)
                    {
                        return 0.0f;
                    }
                }
                else
                {
                    // If we're in a container (e.g. held or whatever) then we probably can't get it. Only exception
                    // Is a locker / crate
                    return 0.0f;
                }
            }

            return 1.0f;
        }
    }
}
