#nullable enable
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Kitchen
{
    public abstract class SharedKitchenSpikeComponent : Component, IDragDropOn
    {
        public override string Name => "KitchenSpike";

        [ViewVariables] [DataField("delay")] protected float SpikeDelay = 10;

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            if (!eventArgs.Dragged.TryGetComponent<IMobStateComponent>(out var state))
            {
                return false;
            }

            // TODO: Wouldn't we just need the CanMove check?
            if (state.IsDead() || state.IsCritical() || state.IsIncapacitated() || !ActionBlockerSystem.CanMove(eventArgs.Dragged))
            {
                return true;
            }

            return false;
        }

        public abstract bool DragDropOn(DragDropEventArgs eventArgs);
    }
}
