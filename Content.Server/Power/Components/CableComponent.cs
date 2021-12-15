using System.Threading.Tasks;
using Content.Server.Electrocution;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Shared.Interaction;
using Content.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Allows the attached entity to be destroyed by a cutting tool, dropping a piece of cable.
    /// </summary>
    [RegisterComponent]
    public class CableComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "Cable";

        [ViewVariables]
        [DataField("cableDroppedOnCutPrototype")]
        private string? _cableDroppedOnCutPrototype = "CableHVStack1";

        [DataField("cuttingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        private string _cuttingQuality = "Cutting";

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

            if (!await EntitySystem.Get<ToolSystem>().UseTool(eventArgs.Using, eventArgs.User, Owner, 0f, 0.25f, _cuttingQuality)) return false;

            if (EntitySystem.Get<ElectrocutionSystem>().TryDoElectrifiedAct(Owner, eventArgs.User)) return false;

            _entMan.DeleteEntity(Owner);
            var droppedEnt = _entMan.SpawnEntity(_cableDroppedOnCutPrototype, eventArgs.ClickLocation);

            // TODO: Literally just use a prototype that has a single thing in the stack, it's not that complicated...
            if (_entMan.TryGetComponent<StackComponent?>(droppedEnt, out var stack))
                EntitySystem.Get<StackSystem>().SetCount(droppedEnt, 1, stack);

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
