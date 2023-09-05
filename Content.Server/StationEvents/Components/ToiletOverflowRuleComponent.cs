using Content.Server.StationEvents.Events;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(VentClogRule))]
public sealed class ToiletOverflowRuleComponent : Component
{
    /* <summary>
    Basically taking this from vent clog for now.
     </summary> */
    [DataField("safeishToiletChemicals", customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    public readonly IReadOnlyList<string> SafeishToiletChemicals = new[]
    {
        "Water", "Toxin", "Slime", "Hooch"  /// other?
    };

    /// <summary>
    /// Sound played when toilet overflows.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg"); /// come back

    /// <summary>
    /// The standard reagent quantity to put in the puddle, modfied by event severity.
    /// </summary>
    [DataField("reagentQuantity"), ViewVariables(VVAccess.ReadWrite)]
    public int ReagentQuantity = 2000;

    /// <summary>
    /// The standard spreading of the puddle, not modfied by event severity.
    /// </summary>
    [DataField("spread"), ViewVariables(VVAccess.ReadWrite)]
    public int Spread = 200;

    /* <summary>
    /// How long the puddle lasts for...maybe remove
    /// </summary>
    [DataField("time"), ViewVariables(VVAccess.ReadWrite)]
    public float Time = 20f; */
}
