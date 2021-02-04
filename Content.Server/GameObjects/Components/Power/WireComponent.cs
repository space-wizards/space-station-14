using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
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

        /// <summary>
        ///     Checked by <see cref="WirePlacerComponent"/> to determine if there is
        ///     already a wire of a type on a tile.
        /// </summary>
        [ViewVariables]
        public WireType WireType => _wireType;
        private WireType _wireType;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _wireDroppedOnCutPrototype, "wireDroppedOnCutPrototype", "HVWireStack1");
            serializer.DataField(ref _wireType, "wireType", WireType.HighVoltage);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out ToolComponent tool)) return false;
            if (!await tool.UseTool(eventArgs.User, Owner, 0.25f, ToolQuality.Cutting)) return false;

            Owner.Delete();
            var droppedEnt = Owner.EntityManager.SpawnEntity(_wireDroppedOnCutPrototype, eventArgs.ClickLocation);

            if (droppedEnt.TryGetComponent<StackComponent>(out var stackComp))
                stackComp.Count = 1;

            return true;
        }
    }

    public enum WireType
    {
        HighVoltage,
        MediumVoltage,
        Apc,
    }
}
