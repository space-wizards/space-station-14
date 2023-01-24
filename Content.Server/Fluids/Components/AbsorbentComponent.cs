using Content.Server.Fluids.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Fluids.Components;

/// <summary>
/// For entities that can clean up puddles
/// </summary>
[RegisterComponent, Access(typeof(MoppingSystem))]
public sealed class AbsorbentComponent : Component
{
    public const string SolutionName = "absorbed";

    [DataField("pickupAmount")]
    public FixedPoint2 PickupAmount = FixedPoint2.New(10);

    /// <summary>
    ///     When using this tool on an empty floor tile, leave this much reagent as a new puddle.
    /// </summary>
    [DataField("residueAmount")]
    public FixedPoint2 ResidueAmount = FixedPoint2.New(10); // Should be higher than MopLowerLimit

    /// <summary>
    ///     To leave behind a wet floor, this tool will be unable to take from puddles with a volume less than this
    ///     amount. This limit is ignored if the target puddle does not evaporate.
    /// </summary>
    [DataField("lowerLimit")]
    public FixedPoint2 LowerLimit = FixedPoint2.New(5);

    [DataField("pickupSound")]
    public SoundSpecifier PickupSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");

    [DataField("transferSound")]
    public SoundSpecifier TransferSound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");

    /// <summary>
    ///     Quantity of reagent that this mop can pick up per second. Determines the length of the do-after.
    /// </summary>
    [DataField("speed")] public float Speed = 10;

    /// <summary>
    ///     How many entities can this tool interact with at once?
    /// </summary>
    [DataField("maxEntities")]
    public int MaxInteractingEntities = 1;

    /// <summary>
    ///     What entities is this tool interacting with right now?
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> InteractingEntities = new();

}
