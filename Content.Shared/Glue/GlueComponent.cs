using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Glue;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedGlueSystem))]
public sealed partial class GlueComponent : Component
{
    /// <summary>
    /// Noise made when glue applied.
    /// </summary>
    [DataField("squeeze")]
    public SoundSpecifier Squeeze = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

    /// <summary>
    /// Solution on the entity that contains the glue.
    /// </summary>
    [DataField("solution")]
    public string Solution = "drink";

    /// <summary>
    /// Reagent that will be used as glue.
    /// </summary>
    [DataField("reagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string Reagent = "SpaceGlue";

    /// <summary>
    /// Reagent consumption per use.
    /// </summary>
    [DataField("consumptionUnit"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 ConsumptionUnit = FixedPoint2.New(5);

    /// <summary>
    /// Duration per unit
    /// </summary>
    [DataField("durationPerUnit"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DurationPerUnit = TimeSpan.FromSeconds(6);
}
