using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Content.Server.DeviceLinking.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// This component allows the facility to register the weight of objects above it and provide signals to devices
/// </summary>
[RegisterComponent, Access(typeof(PressurePlateSystem))]
public sealed partial class PressurePlateComponent : Component
{
    [DataField]
    public bool IsPressed;

    /// <summary>
    /// The required weight of an object that happens to be above the slab to activate.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WeightRequired = 100f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float CurrentWeight;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> PressedPort = "Pressed";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> StatusPort = "Status";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> ReleasedPort = "Released";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier PressedSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ReleasedSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
