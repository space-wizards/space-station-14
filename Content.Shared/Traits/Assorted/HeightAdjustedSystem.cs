
using Robust.Shared.Network;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles...
/// </summary>
public sealed class HeightAdjustedSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HeightAdjustedComponent, ComponentStartup>(SetupHeight);
    }

    private void SetupHeight(EntityUid uid, HeightAdjustedComponent component, ComponentStartup args)
    {
        if (_netManager.IsClient && !uid.IsClientSide())
            return; // This is so the trait works in the character editor without stomping on server state.

        EnsureComp<ScaleVisualsComponent>(uid);
        if (!_appearance.TryGetData(uid, ScaleVisuals.Scale, out var oldScale))
            oldScale = Vector2.One;

        _appearance.SetData(uid, ScaleVisuals.Scale, (Vector2)oldScale * new Vector2(1.0f, component.Height));
    }
}
