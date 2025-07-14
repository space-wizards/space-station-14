using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components
{
    /// <summary>
    /// An industrial grade chemical manipulator with pill and bottle production included.
    /// <seealso cref="ChemMasterSystem"/>
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(ChemMasterSystem))]
    public sealed partial class ChemMasterComponent : Component
    {
        [DataField("pillType"), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public uint PillType = 0;

        [DataField("mode"), AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public ChemMasterMode Mode = ChemMasterMode.Transfer;

        [DataField, AutoNetworkedField]
        public ChemMasterSortingType SortingType = ChemMasterSortingType.None;

        [DataField("pillDosageLimit", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint PillDosageLimit;

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
    }
}
