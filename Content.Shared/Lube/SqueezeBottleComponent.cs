using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Lube;

[RegisterComponent, NetworkedComponent]
public sealed partial class SqueezeBottleComponent : Component
{
    [DataField("OnSqueezeNoise")]
    public SoundSpecifier OnSqueezeNoise = new SoundPathSpecifier("/Audio/Items/squeezebottle.ogg");

    /// <summary>
    /// Solution on the entity that contains the glue.
    /// </summary>
    [DataField("solution")]
    public string Solution = "drink";

    /// <summary>
    /// Reagent consumption per use.
    /// </summary>
    [DataField("AmountConsumedOnUse"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 AmountConsumedOnUse = FixedPoint2.New(5);
}
