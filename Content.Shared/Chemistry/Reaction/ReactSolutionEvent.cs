
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Chemistry.Reaction;

public sealed class ReactSolutionEvent : EntityEventArgs
{
    public Solution Solution;
    public FixedPoint2 MaxVolume;
    public EntityUid Owner;
    public EntityUid? ContainerHolder;
    public ReactionMixerComponent? MixerComponent;

    public ReactSolutionEvent(Solution solution, FixedPoint2 maxVolume, EntityUid owner, EntityUid? containerHolder = null, ReactionMixerComponent? mixerComponent = null)
    {
        Solution = solution;
        MaxVolume = maxVolume;
        MixerComponent = mixerComponent;
        Owner = owner;
        ContainerHolder = containerHolder;
    }
}

/// <summary>
///     Raised directed at the owner of a solution to determine whether the reaction should be allowed to occur.
/// </summary>
/// <reamrks>
///     Some solution containers (e.g., bloodstream, smoke, foam) use this to block certain reactions from occurring.
/// </reamrks>
public sealed class ReactionAttemptEvent : CancellableEntityEventArgs
{
    public readonly ReactionPrototype Reaction;
    public readonly Solution Solution;

    public ReactionAttemptEvent(ReactionPrototype reaction, Solution solution)
    {
        Reaction = reaction;
        Solution = solution;
    }
}