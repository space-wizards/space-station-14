using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Client.Chemistry.EntitySystems;

/// <inheritdoc/>
public sealed class ThermobathSystem : SharedThermobathSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermobathComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<ThermobathComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<ThermobathComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, ThermobathUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
