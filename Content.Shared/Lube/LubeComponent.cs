using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Lube;

[RegisterComponent, NetworkedComponent]
public sealed partial class LubeComponent : Component
{
    [DataField]
    public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

    /// <summary>
    /// Solution on the entity that contains the glue.
    /// </summary>
    [DataField]
    public string Solution = "drink";

    /// <summary>
    /// Reagent that will be used as glue.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>)))]
    public string Reagent = "SpaceLube";

    /// <summary>
    /// Reagent consumption per use.
    /// </summary>
    [DataField, ViewVariables]
    public FixedPoint2 Consumption = FixedPoint2.New(3);

    /// <summary>
    /// Min slips per unit
    /// </summary>
    [DataField, ViewVariables]
    public int MinSlips = 1;

    /// <summary>
    /// Max slips per unit
    /// </summary>
    [DataField, ViewVariables]
    public int MaxSlips = 6;

    [DataField, ViewVariables]
    public int SlipStrength = 10;
}
