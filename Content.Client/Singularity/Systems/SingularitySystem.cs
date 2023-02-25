using Content.Shared.Singularity;
using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Client.Singularity.EntitySystems;

/// <summary>
/// The client-side version of <see cref="SharedSingularitySystem"/>.
/// Primarily manages <see cref="SingularityComponent"/>s.
/// </summary>
public sealed class SingularitySystem : SharedSingularitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SingularityComponent, ComponentHandleState>(HandleSingularityState);
        SubscribeLocalEvent<SingularityComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    /// <summary>
    /// Handles syncing singularities with their server-side versions.
    /// </summary>
    /// <param name="uid">The uid of the singularity to sync.</param>
    /// <param name="comp">The state of the singularity to sync.</param>
    /// <param name="args">The event arguments including the state to sync the singularity with.</param>
    private void HandleSingularityState(EntityUid uid, SingularityComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not SingularityComponentState state)
            return;

        SetLevel(uid, state.Level, comp);
    }

    /// <summary>
    /// Handles ensuring that the singularity has a sprite to see.
    /// </summary>
    protected override void OnSingularityStartup(EntityUid uid, SingularityComponent comp, ComponentStartup args)
    {
        base.OnSingularityStartup(uid, comp, args);
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.LayerMapReserveBlank(comp.Layer);
        }
    }

    /// <summary>
    /// Handles updating the visible state of the singularity to reflect its current level.
    /// </summary>
    private void OnAppearanceChange(EntityUid uid, SingularityComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if(!_appearanceSystem.TryGetData<byte>(uid, SingularityVisuals.Level, out var level, args.Component))
            return;

        args.Sprite.LayerSetSprite(comp.Layer,
            new SpriteSpecifier.Rsi(new ResourcePath($"{comp.BaseSprite.RsiPath}_{level}.rsi"), $"{comp.BaseSprite.RsiState}_{level}"));
    }
}
