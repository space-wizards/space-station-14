#nullable enable
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Allows the attached entity to be destroyed by a cutting tool, dropping a piece of cable.
    /// </summary>
    [RegisterComponent]
    public class CableComponent : Component, IInteractUsing
    {
        public override string Name => "Cable";

        [ViewVariables]
        [DataField("cableDroppedOnCutPrototype")]
        private string? _cableDroppedOnCutPrototype = "CableHVStack1";

        /// <summary>
        ///     Checked by <see cref="CablePlacerComponent"/> to determine if there is
        ///     already a cable of a type on a tile.
        /// </summary>
        [ViewVariables]
        public CableType CableType => _cableType;
        [DataField("cableType")]
        private CableType _cableType = CableType.HighVoltage;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_cableDroppedOnCutPrototype == null)
                return false;

            if (!eventArgs.Using.TryGetComponent<ToolComponent>(out var tool)) return false;
            if (!await tool.UseTool(eventArgs.User, Owner, 0.25f, ToolQuality.Cutting)) return false;

            Owner.Delete();
            var droppedEnt = Owner.EntityManager.SpawnEntity(_cableDroppedOnCutPrototype, eventArgs.ClickLocation);

            // TODO: Literally just use a prototype that has a single thing in the stack, it's not that complicated...
            if (droppedEnt.TryGetComponent<StackComponent>(out var stack))
                EntitySystem.Get<StackSystem>().SetCount(droppedEnt.Uid, stack, 1);

            return true;
        }
    }

    public enum CableType
    {
        HighVoltage,
        MediumVoltage,
        Apc,
    }
}
