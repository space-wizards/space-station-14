using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ApplyReagentToItem;

[RegisterComponent, NetworkedComponent]
public sealed partial class ApplyReagentToItemComponent : Component
{
    /// <summary>
    ///     The noise the squeeze bottle makes when it gets squeezed!
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier OnSqueezeNoise = default!;

    /// <summary>
    ///     Solution on the entity that contains the glue.
    /// </summary>
    [DataField]
    public string Solution = "drink";

    /// <summary>
    ///     Reagent consumption per use.
    /// </summary>
    [DataField]
    public FixedPoint2 AmountConsumedOnUse = FixedPoint2.New(5);
}
