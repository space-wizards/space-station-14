#nullable enable
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Kitchen
{
    public abstract class SharedKitchenSpikeComponent : Component, IDragDropOn
    {
        public override string Name => "KitchenSpike";

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            if (!eventArgs.Dragged.TryGetComponent<IMobStateComponent>(out var state))
            {
                return false;
            }

            if (state.IsDead() || state.IsCritical() || state.IsIncapacitated() || !ActionBlockerSystem.CanMove(eventArgs.Dragged))
            {
                return true;
            }

            return false;
        }

        public virtual bool DragDropOn(DragDropEventArgs eventArgs)
        {
            return true;
        }

    }
}
