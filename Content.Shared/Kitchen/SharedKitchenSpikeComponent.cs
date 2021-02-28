#nullable enable
using Content.Shared.GameObjects.Components.Mobs;
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

        [ViewVariables(VVAccess.ReadWrite)]
        protected string? SpikeSound;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref SpikeDelay, "delay", 12.0f);
            serializer.DataField(ref SpikeSound, "sound", "/Audio/Effects/Fluids/splat.ogg");
        }

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            if (eventArgs.User == eventArgs.Dragged ||
                !eventArgs.Dragged.TryGetComponent<IMobStateComponent>(out var state) ||
                (eventArgs.User.TryGetComponent(out SharedCombatModeComponent? combatMode) && !combatMode.IsInCombatMode))
            {
                return false;
            }

            // TODO: Once we get silicons need to check organic
            return !state.IsDead();
        }

        public abstract bool DragDropOn(DragDropEventArgs eventArgs);
    }
}
