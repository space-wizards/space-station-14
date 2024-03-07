using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// A Drain allows an entity to absorb liquid in a disposal goal. Drains can be filled manually (with the Empty verb)
/// or they can absorb puddles of liquid around them when AutoDrain is set to true.
/// When the entity also has a SolutionContainerManager attached with a solution named drainBuffer, this solution
/// gets filled until the drain is full.
/// When the drain is full, it can be unclogged using a plunger (i.e. an entity with a Plunger tag attached).
/// Later this can be refactored into a proper Plunger component if needed.
/// </summary>
[RegisterComponent, Access(typeof(SharedDrainSystem))]
public sealed partial class DrainComponent : Component
{
    public const string SolutionName = "drainBuffer";

    [ValidatePrototypeId<TagPrototype>]
    public const string PlungerTag = "Plunger";

    [DataField]
    public Entity<SolutionComponent>? Solution = null;

    [DataField("accumulator")]
    public float Accumulator = 0f;

    /// <summary>
    /// Does this drain automatically absorb surrouding puddles? Or is it a drain designed to empty
    /// solutions in it manually?
    /// </summary>
    [DataField("autoDrain"), ViewVariables(VVAccess.ReadOnly)]
    public bool AutoDrain = true;

    /// <summary>
    /// How many units per second the drain can absorb from the surrounding puddles.
    /// Divided by puddles, so if there are 5 puddles this will take 1/5 from each puddle.
    /// This will stay fixed to 1 second no matter what DrainFrequency is.
    /// </summary>
    [DataField("unitsPerSecond")]
    public float UnitsPerSecond = 6f;

    /// <summary>
    /// How many units are ejected from the buffer per second.
    /// </summary>
    [DataField("unitsDestroyedPerSecond")]
    public float UnitsDestroyedPerSecond = 3f;

    /// <summary>
    /// How many (unobstructed) tiles away the drain will
    /// drain puddles from.
    /// </summary>
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 2f;

    /// <summary>
    /// How often in seconds the drain checks for puddles around it.
    /// If the EntityQuery seems a bit unperformant this can be increased.
    /// </summary>
    [DataField("drainFrequency")]
    public float DrainFrequency = 1f;

    /// <summary>
    /// How much time it takes to unclog it with a plunger
    /// </summary>
    [DataField("unclogDuration"), ViewVariables(VVAccess.ReadWrite)]
    public float UnclogDuration = 1f;

    /// <summary>
    /// What's the probability of uncloging on each try
    /// </summary>
    [DataField("unclogProbability"), ViewVariables(VVAccess.ReadWrite)]
    public float UnclogProbability = 0.75f;

    [DataField("manualDrainSound"), ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier ManualDrainSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");

    [DataField("plungerSound"), ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier PlungerSound = new SoundPathSpecifier("/Audio/Items/Janitor/plunger.ogg");

    [DataField("unclogSound"), ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier UnclogSound = new SoundPathSpecifier("/Audio/Effects/Fluids/glug.ogg");
}
