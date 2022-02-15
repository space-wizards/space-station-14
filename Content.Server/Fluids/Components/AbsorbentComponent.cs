using System.Threading.Tasks;
using System.Threading;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;


namespace Content.Server.Fluids.Components;

/// <summary>
/// For entities that can clean up puddles
/// </summary>
[RegisterComponent, Friend(typeof(MoppingSystem))]
public class AbsorbentComponent : Component
{
    [Dependency] private readonly IEntityManager _entities = default!;

     public const string SolutionName = "absorbed";

    /// <summary>
    ///     Used to prevent do_after spam if we're currently absorbing.
    /// </summary>
    public bool CurrentlyAbsorbing { get; set; }

    // AbsorbedSolution stores whatever solution the entity has absorbed.
    public Solution? AbsorbedSolution
    {
        get
        {
            EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution);
            return solution;
        }
    }

    // MaxVolume is the Maximum volume the mop can absorb (however, this is defined in janitor.yml)
    public FixedPoint2 MaxVolume
    {
        get => AbsorbedSolution?.MaxVolume ?? FixedPoint2.Zero;
        set
        {
            var solution = AbsorbedSolution;
            if (solution != null)
            {
                solution.MaxVolume = value;
            }
        }
    }

    // CurrentVolume is the volume the mop has absorbed.
    public FixedPoint2 CurrentVolume => AbsorbedSolution?.CurrentVolume ?? FixedPoint2.Zero;

    // AvailableVolume is the remaining volume capacity of the mop.
    public FixedPoint2 AvailableVolume => AbsorbedSolution?.AvailableVolume ?? FixedPoint2.Zero;

    // Currently there's a separate amount for pickup and dropoff so
    // Picking up a puddle requires multiple clicks
    // Dumping in a bucket requires 1 click
    // Long-term you'd probably use a cooldown and start the pickup once we have some form of global cooldown
    [DataField("pickupAmount")]
    public FixedPoint2 PickupAmount { get; } = FixedPoint2.New(10);

    /// <summary>
    ///     When using the mop on an empty floor tile, leave this much reagent as a new puddle.
    /// </summary>
    [DataField("residueAmount")]
    public FixedPoint2 ResidueAmount { get; } = FixedPoint2.New(10); // Should be higher than MopLowerLimit

    /// <summary>
    ///     To leave behind a wet floor, the mop will be unable to take from puddles with a volume less than this amount.
    /// </summary>
    [DataField("mopLowerLimit")]
    public FixedPoint2 MopLowerLimit { get; } = FixedPoint2.New(5);

    [DataField("pickupSound")]
    public SoundSpecifier PickupSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");

    /// <summary>
    ///     Multiplier for the do_after delay for how fast the mop works.
    /// </summary>
    [ViewVariables]
    [DataField("mopSpeed")] public float MopSpeed = 1;

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
