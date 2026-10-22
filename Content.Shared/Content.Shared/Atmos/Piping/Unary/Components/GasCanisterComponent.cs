using Content.Shared.Atmos.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Guidebook;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Piping.Unary.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasCanisterComponent : GasMaxPressureHolderComponent
{
    [DataField("port")]
    public string PortName { get; set; } = "port";

    /// <summary>
    ///     Container name for the gas tank holder.
    /// </summary>
    [DataField("container")]
    public string ContainerName { get; set; } = "tank_slot";

    [DataField]
    public ItemSlot GasTankSlot = new();

    /// <summary>
    /// The safety release valve on this gas canister. Automatically opens
    /// when <see cref="GasMaxPressureHolderComponent.SafetyPressure"/> is reached.
    /// </summary>
    [DataField]
    public bool SafetyValveOpen;

    /// <summary>
    ///     Last recorded pressure, for appearance-updating purposes.
    /// </summary>
    public float LastPressure = 0f;

    [GuidebookData]
    public float Volume => Air.Volume;
}
