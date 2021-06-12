#nullable enable
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Wires.Components
{
    /// <summary>
    ///     Allows the attached entity to be destroyed by a cutting tool, dropping a piece of wire.
    /// </summary>
    [RegisterComponent]
    public class WireComponent : Component, IInteractUsing
    {
        public override string Name => "Wire";

        [ViewVariables]
        [DataField("wireDroppedOnCutPrototype")]
        private string? _wireDroppedOnCutPrototype = "HVWireStack1";

        /// <summary>
        ///     Checked by <see cref="WirePlacerComponent"/> to determine if there is
        ///     already a wire of a type on a tile.
        /// </summary>
        [ViewVariables]
        public WireType WireType => _wireType;
        [DataField("wireType")]
        private WireType _wireType = WireType.HighVoltage;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_wireDroppedOnCutPrototype == null)
                return false;

            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool)) return false;
            if (!await tool.UseTool(eventArgs.User, Owner, 0.25f, ToolQuality.Cutting)) return false;

            Owner.Delete();
            var droppedEnt = Owner.EntityManager.SpawnEntity(_wireDroppedOnCutPrototype, eventArgs.ClickLocation);

            // TODO: Literally just use a prototype that has a single thing in the stack, it's not that complicated...
            if (droppedEnt.TryGetComponent<StackComponent>(out var stack))
                EntitySystem.Get<StackSystem>().SetCount(droppedEnt.Uid, stack, 1);

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
