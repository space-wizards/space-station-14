using Content.Shared.Creatures.SpaceLeech;
using Robust.Shared.GameStates;

namespace Content.Client.Creatures.SpaceLeech;

/// <summary>
/// Refreshes the open upgrade menu whenever new <see cref="SpaceLeechComponent"/> state arrives.
/// The menu is driven entirely by the networked component; there is no separate BUI state.
/// </summary>
public sealed partial class SpaceLeechUiSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceLeechComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<SpaceLeechComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<SpaceLeechUpgradeMenuBoundUserInterface>(ent.Owner, SpaceLeechUiKey.UpgradeMenu, out var bui))
            bui.Refresh();
    }
}
