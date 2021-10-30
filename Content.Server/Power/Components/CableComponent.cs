using System.Threading.Tasks;
using Content.Server.Electrocution;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
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

            if (!await EntitySystem.Get<ToolSystem>().UseTool(eventArgs.Using.Uid, eventArgs.User.Uid, Owner.Uid, 0f, 0.25f, _cuttingQuality)) return false;

            if (EntitySystem.Get<ElectrocutionSystem>().TryDoElectrifiedAct(Owner.Uid, eventArgs.User.Uid)) return false;

            Owner.Delete();
            var droppedEnt = Owner.EntityManager.SpawnEntity(_cableDroppedOnCutPrototype, eventArgs.ClickLocation);

            // TODO: Literally just use a prototype that has a single thing in the stack, it's not that complicated...
            if (droppedEnt.TryGetComponent<StackComponent>(out var stack))
                EntitySystem.Get<StackSystem>().SetCount(droppedEnt.Uid, 1, stack);

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
