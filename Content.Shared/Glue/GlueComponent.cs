using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Glue;

[RegisterComponent, NetworkedComponent]
public sealed class GlueComponent : Component
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
    [DataField("consumption"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Consumption = FixedPoint2.New(3);

    /// <summary>
    /// Duration per unit
    /// </summary>
    [DataField("duration", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);
}
