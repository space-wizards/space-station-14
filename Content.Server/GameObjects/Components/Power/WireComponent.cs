using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Allows the attached entity to be destroyed by a cutting tool, dropping a piece of wire.
    /// </summary>
    [RegisterComponent]
    public class WireComponent : Component, IInteractUsing
    {
        public override string Name => "Wire";

        [ViewVariables]
        private string _wireDroppedOnCutPrototype;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _wireDroppedOnCutPrototype, "wireDroppedOnCutPrototype", "HVWireStack1");
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out ToolComponent tool)) return false;
            if (!tool.UseTool(eventArgs.User, Owner, ToolQuality.Cutting)) return false;

            Owner.Delete();
            var droppedEnt = Owner.EntityManager.SpawnEntity(_wireDroppedOnCutPrototype, eventArgs.ClickLocation);

            if (droppedEnt.TryGetComponent<StackComponent>(out var stackComp))
                stackComp.Count = 1;

            return true;
        }
    }
}
