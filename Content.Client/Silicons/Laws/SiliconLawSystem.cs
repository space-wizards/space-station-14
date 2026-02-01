using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, AfterAutoHandleStateEvent>(AfterAutoHandleState);
    }

    private void AfterAutoHandleState(Entity<SiliconLawBoundComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi(ent.Owner, SiliconLawsUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
