using Robust.Server.GameObjects;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles...
/// </summary>
public sealed class HeightAdjustedSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HeightAdjustedComponent, ComponentStartup>(SetupHeight);
    }

    private void SetupHeight(EntityUid uid, HeightAdjustedComponent component, ComponentStartup args)
    {
        EnsureComp<ScaleVisualsComponent>(uid);
        EnsureComp<ServerAppearanceComponent>(uid);
        if (!_appearance.TryGetData(uid, ScaleVisuals.Scale, out var oldScale))
            oldScale = Vector2.One;

        _appearance.SetData(uid, ScaleVisuals.Scale, (Vector2)oldScale * new Vector2(1.0f, component.Height));
    }
}
