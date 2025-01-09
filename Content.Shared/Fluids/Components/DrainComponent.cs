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

    [DataField]
    public float Accumulator = 0f;

    /// <summary>
    /// If true, automatically transfers solutions from nearby puddles and drains them. True for floor drains;
    /// false for things like toilets and sinks.
    /// </summary>
    [DataField]
    public bool AutoDrain = true;

    /// <summary>
    /// How many units per second the drain can absorb from the surrounding puddles.
    /// Divided by puddles, so if there are 5 puddles this will take 1/5 from each puddle.
    /// This will stay fixed to 1 second no matter what DrainFrequency is.
    /// </summary>
    [DataField]
    public float UnitsPerSecond = 6f;

    /// <summary>
    /// How many units are ejected from the buffer per second.
    /// </summary>
    [DataField]
    public float UnitsDestroyedPerSecond = 3f;

    /// <summary>
    /// How many (unobstructed) tiles away the drain will
    /// drain puddles from.
    /// </summary>
    [DataField]
    public float Range = 2.5f;

    /// <summary>
    /// How often in seconds the drain checks for puddles around it.
    /// If the EntityQuery seems a bit unperformant this can be increased.
    /// </summary>
    [DataField]
    public float DrainFrequency = 1f;

    /// <summary>
    /// How much time it takes to unclog it with a plunger
    /// </summary>
    [DataField]
    public float UnclogDuration = 1f;

    /// <summary>
    /// What's the probability of uncloging on each try
    /// </summary>
    [DataField]
    public float UnclogProbability = 0.75f;

    [DataField]
    public SoundSpecifier ManualDrainSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");

    [DataField]
    public SoundSpecifier PlungerSound = new SoundPathSpecifier("/Audio/Items/Janitor/plunger.ogg");

    [DataField]
    public SoundSpecifier UnclogSound = new SoundPathSpecifier("/Audio/Effects/Fluids/glug.ogg");
}
