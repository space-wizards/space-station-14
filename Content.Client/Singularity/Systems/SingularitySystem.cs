using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Client.Singularity.Systems;

/// <summary>
/// The client-side version of <see cref="SharedSingularitySystem"/>.
/// Primarily manages <see cref="SingularityComponent"/>s.
/// </summary>
public sealed class SingularitySystem : SharedSingularitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SingularityComponent, ComponentHandleState>(HandleSingularityState);
    }

    /// <summary>
    /// Handles syncing singularities with their server-side versions.
    /// </summary>
    /// <param name="uid">The uid of the singularity to sync.</param>
    /// <param name="comp">The state of the singularity to sync.</param>
    /// <param name="args">The event arguments including the state to sync the singularity with.</param>
    private void HandleSingularityState(Entity<SingularityComponent> singularity, ref ComponentHandleState args)
    {
        if (args.Current is not SingularityComponentState state)
            return;

        SetLevel(singularity.AsNullable(), state.Level);
    }
}
