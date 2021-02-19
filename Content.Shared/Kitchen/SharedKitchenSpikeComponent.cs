#nullable enable
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Kitchen
{
    public abstract class SharedKitchenSpikeComponent : Component, IDragDropOn
    {
        public override string Name => "KitchenSpike";

        [ViewVariables]
        protected float SpikeDelay;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref SpikeDelay, "delay", 10.0f);
        }

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
