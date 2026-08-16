using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// An industrial grade chemical manipulator with pill and bottle production included.
/// <seealso cref="Content.Shared.Chemistry.EntitySystems.SharedChemMasterSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedChemMasterSystem))]
public sealed partial class ChemMasterComponent : Component
{
    [DataField]
    public uint PillType;

    [DataField, AutoNetworkedField]
    public ChemMasterMode Mode = ChemMasterMode.Transfer;

    [DataField, AutoNetworkedField]
    public ChemMasterSortingType SortingType = ChemMasterSortingType.None;

    [DataField(required: true)]
    public uint PillDosageLimit;

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// Which source the chem master should draw from when making pills/bottles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ChemMasterDrawSource DrawSource = ChemMasterDrawSource.Internal;
}
